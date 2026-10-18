using JegoroWordleHeroes.Areas.Identity.Data;
using JegoroWordleHeroes.Models;
using JegoroWordleHeroes.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JegoroWordleHeroes.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameRegistry _registry;
        private readonly WordService _words;
        private readonly UserManager<JegorowordleHeroesUser> _userManager;
        private readonly MatchmakingQueue _queue;
        private readonly JegorowordleHeroesContext _db;
        private const int MaxChatLength = 200;
        private const int MaxNameLength = 12;

        

        public GameHub(GameRegistry registry, WordService words, UserManager<JegorowordleHeroesUser> userManager, MatchmakingQueue queue, JegorowordleHeroesContext db)
        {
            _registry = registry;
            _words = words;
            _userManager = userManager;
            _queue = queue;
            _db = db;
        }
        

        public async Task<string> CreateOrJoin(string roomCode, string playerId, string playerName, bool solo)
        {
            playerName = (playerName ?? "").Trim();
            if (playerName.Length == 0) playerName = "Spieler";
            if (playerName.Length > MaxNameLength) playerName = playerName.Substring(0, MaxNameLength);

            var isRegistered = Context.UserIdentifier != null;
            var avatar = 0;
            playerId = ResolvePlayerId(playerId);
            if (isRegistered)
            {
                playerName = Context.User?.Identity?.Name ?? playerName;

                var user = await _userManager.FindByIdAsync(playerId);
                avatar = user?.Avatar ?? 0;
            }

            var session = _registry.GetOrCreate(roomCode, _words.PickWord(), solo);

            if (session.IsOver && !session.HasPlayer(playerId) && !session.HasConnectedPlayers)
            {
                session = _registry.StartNew(roomCode, _words.PickWord(), solo);
            }

            var player = session.AddOrReconnectPlayer(Context.ConnectionId, playerId, playerName, isRegistered);

            if (player is null)
            {
                await Clients.Caller.SendAsync("RoomFull");
                return string.Empty;
            }

            player.Avatar = avatar;
            session.LastActivity = DateTime.UtcNow;

            await Clients.Caller.SendAsync("Joined", player.PublicId);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Caller.SendAsync("RestoreState", player.Guesses);
            await Clients.Group(roomCode).SendAsync("PlayersUpdated",
                    PlayerInfo(session.PlayerA), PlayerInfo(session.PlayerB));
            if (session.IsReady && !session.IsOver)
            {
                await Clients.Group(roomCode).SendAsync("GameReady", GameSession.MaxGuesses, 5);
            }
            if (session.IsOver)
            {
                await Clients.Caller.SendAsync("GameOver", BuildGameOver(session, restored: true));
            }
            return player.PublicId;
        }

        public string CreateSoloRoom() => _registry.CreateRandomRoomCode();

        public async Task FindOpponent(string playerId)
        {
            playerId = ResolvePlayerId(playerId);

            var opponent = _queue.TryMatch(new WaitingPlayer(Context.ConnectionId, playerId));
            if (opponent is null) return;

            var roomCode = _registry.CreateRandomRoomCode();
            await Clients.Clients(opponent.ConnectionId, Context.ConnectionId).SendAsync("Matched", roomCode);
        }

        public async Task SubmitGuess(string roomCode, string guess)
        {
            var session = _registry.Get(roomCode);
            if (session is null) return;
            session.LastActivity = DateTime.UtcNow;

            var player = session.FindByConnection(Context.ConnectionId);
            if (player is null || session.IsOver) return;

            if (!session.IsReady)
            {
                await Clients.Caller.SendAsync("WaitingForOpponent");
                return;
            }

            guess = (guess ?? "").Trim().ToLower();

            if (!_words.IsValid(guess) || guess.Length != 5)
            {
                await Clients.Caller.SendAsync("InvalidGuess");
                return;
            }

            var outcome = session.TryGuess(player, guess);
            if (outcome is null)
            {
                if (!session.IsOver) await Clients.Caller.SendAsync("NoGuessesLeft");
                return;
            }

            await Clients.Caller.SendAsync("GuessAccepted", outcome.Result);
            await Clients.GroupExcept(roomCode, new[] { Context.ConnectionId })
                         .SendAsync("OpponentGuessed", player.Name, outcome.GuessCount, player.PublicId);

            if (!outcome.EndedGame) return;

            if (session.Winner == player && player.IsRegistered && !session.IsSolo)
            {
                await AddScore(player.Id, session.WinnerPoints);
            }
            await Clients.Group(roomCode).SendAsync("GameOver", BuildGameOver(session));
        }

        public async Task RequestRematch(string roomCode)
        {
            var session = _registry.Get(roomCode);
            if (session is null || !session.IsOver) return;
            session.LastActivity = DateTime.UtcNow;

            var player = session.FindByConnection(Context.ConnectionId);
            if (player is null) return;

            var opponent = session.GetOpponent(player);
            if (!session.IsSolo && (opponent is null || opponent.ConnectionId == ""))
            {
                await Clients.Caller.SendAsync("RematchUnavailable");
                return;
            }

            var rematch = session.VoteRematch(player, _words.PickWord());

            if (rematch == RematchResult.Requested && opponent != null)
            {
                await Clients.Client(opponent.ConnectionId).SendAsync("RematchRequested", player.Name);
            }
            else if (rematch == RematchResult.Started)
            {
                await Clients.Group(roomCode).SendAsync("RestoreState", new List<GuessResult>());
                await Clients.Group(roomCode).SendAsync("GameReady", GameSession.MaxGuesses, 5);
            }
        }

        private string ResolvePlayerId(string playerId)
        {
            if (Context.UserIdentifier != null) return Context.UserIdentifier;

            var guestId = Guid.TryParse(playerId, out var parsed) ? parsed : Guid.NewGuid();
            return "guest:" + guestId;
        }

        private async Task AddScore(string userId, int points)
        {
            await _db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Score, u => u.Score + points));
        }

        private static object? PlayerInfo(Player? player)
        {
            if (player is null) return null;

            return new
            {
                Id = player.PublicId,
                player.Name,
                player.Avatar,
                Guesses = player.Guesses.Count,
                Connected = player.ConnectionId != ""
            };
        }

        private static object BuildGameOver(GameSession session, bool restored = false)
        {
            return new
            {
                WinnerName = session.Winner?.Name,
                WinnerId = session.Winner?.PublicId,
                WinnerGuesses = session.Winner?.Guesses.Count ?? 0,
                Points = session.IsSolo ? 0 : session.WinnerPoints,
                ByWin = session.Winner != null,
                TargetWord = session.TargetWord,
                Restored = restored
            };
        }

        public async Task SendChat(string roomCode, string text)
        {
            var session = _registry.Get(roomCode);
            if (session is null) return;

            var player = session.FindByConnection(Context.ConnectionId);
            if (player is null) return;

            text = (text ?? ""). Trim();
            if (text.Length == 0) return;
            // nur für konsolenhacks, wird auch schon client-seitig validiert
            if (text.Length > MaxChatLength)
                text = text.Substring(0, MaxChatLength);
            
            session.LastActivity = DateTime.UtcNow;
            await Clients.Group(roomCode).SendAsync("ChatMessage", player.PublicId, player.Name, text, player.Avatar);            
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _queue.Remove(Context.ConnectionId);

            var session = _registry.RemoveConnection(Context.ConnectionId, out var roomCode, out var playerName);
            if (session != null && roomCode != null)
            {
                await Clients.Group(roomCode).SendAsync("PlayerLeft", playerName);
                await Clients.Group(roomCode).SendAsync("PlayersUpdated",
                    PlayerInfo(session.PlayerA), PlayerInfo(session.PlayerB));
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}