using JegoroWordleHeroes.Areas.Identity.Data;
using JegoroWordleHeroes.Models;
using JegoroWordleHeroes.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace JegoroWordleHeroes.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameRegistry _registry;
        private readonly WordService _words;
        private readonly UserManager<JegorowordleHeroesUser> _userManager;
        private readonly MatchmakingQueue _queue;
        private const int MaxChatLength = 200;
        private const int MaxNameLength = 12;

        

        public GameHub(GameRegistry registry, WordService words, UserManager<JegorowordleHeroesUser> userManager, MatchmakingQueue queue)
        {
            _registry = registry;
            _words = words;
            _userManager = userManager;
            _queue = queue;
        }
        

        public async Task<string> CreateOrJoin(string roomCode, string playerId, string playerName)
        {
            playerName = (playerName ?? "").Trim();
            if (playerName.Length == 0) playerName = "Spieler";
            if (playerName.Length > MaxNameLength) playerName = playerName.Substring(0, MaxNameLength);

            var isRegistered = Context.UserIdentifier != null;
            var avatar = 0;
            if (isRegistered)
            {
                playerId = Context.UserIdentifier!;
                playerName = Context.User?.Identity?.Name ?? playerName;

                var user = await _userManager.FindByIdAsync(playerId);
                avatar = user?.Avatar ?? 0;
            }

            var session = _registry.GetOrCreate(roomCode, _words.PickWord());

            if (session.IsOver && !session.HasPlayer(playerId))
            {
                foreach (var oldPlayer in new[] { session.PlayerA, session.PlayerB })
                {
                    if (oldPlayer != null && oldPlayer.ConnectionId != "")
                        await Groups.RemoveFromGroupAsync(oldPlayer.ConnectionId, roomCode);
                }
                session = _registry.StartNew(roomCode, _words.PickWord());
            }

            var player = session.AddOrReconnectPlayer(Context.ConnectionId, playerId, playerName, isRegistered);

            if (player is null)
            {
                await Clients.Caller.SendAsync("RoomFull");
                return string.Empty;
            }

            player.Avatar = avatar;

            await Clients.Caller.SendAsync("Joined", player.Id);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Caller.SendAsync("RestoreState", player.Guesses);
            await Clients.Group(roomCode).SendAsync("PlayersUpdated",
                    PlayerInfo(session.PlayerA), PlayerInfo(session.PlayerB));
            if (session.IsReady && !session.IsOver)
            {
                await Clients.Group(roomCode).SendAsync("GameReady", 6, 5);
            }
            if (session.IsOver)
            {
                await Clients.Caller.SendAsync("GameOver", BuildGameOver(session, restored: true));
            }
            return player.Id;
        }

        public async Task FindOpponent(string playerId)
        {
            if (Context.UserIdentifier != null)
            {
                playerId = Context.UserIdentifier;
            }

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

            if (player.Guesses.Count >= 6)
            {
                await Clients.Caller.SendAsync("NoGuessesLeft");
                return;
            }

            var result = ScoreGuess(guess, session.TargetWord);
            player.Guesses.Add(result);

            await Clients.Caller.SendAsync("GuessAccepted", result);
            await Clients.GroupExcept(roomCode, new[] { Context.ConnectionId })
                         .SendAsync("OpponentGuessed", player.Name, player.Guesses.Count, player.Id);

            if (result.IsWin)
            {
                session.Winner = player;
                session.IsOver = true;
                if (player.IsRegistered)
                {
                    await AddScore(player.Id, session.WinnerPoints);
                }
                await Clients.Group(roomCode).SendAsync("GameOver", BuildGameOver(session));
                return;
            }

            if (session.AllPlayersExhausted(6))
            {
                session.IsOver = true;
                await Clients.Group(roomCode).SendAsync("GameOver", BuildGameOver(session));
            }
        }

        public async Task RequestRematch(string roomCode)
        {
            var session = _registry.Get(roomCode);
            if (session is null || !session.IsOver) return;

            var player = session.FindByConnection(Context.ConnectionId);
            if (player is null) return;

            var opponent = session.GetOpponent(player);
            if (opponent is null || opponent.ConnectionId == "")
            {
                await Clients.Caller.SendAsync("RematchUnavailable");
                return;
            }

            session.RematchVotes.Add(player.Id);

            if (!session.RematchVotes.Contains(opponent.Id))
            {
                await Clients.Client(opponent.ConnectionId).SendAsync("RematchRequested", player.Name);
                return;
            }

            session.Reset(_words.PickWord());
            await Clients.Group(roomCode).SendAsync("RestoreState", new List<GuessResult>());
            await Clients.Group(roomCode).SendAsync("GameReady", 6, 5);
        }

        private async Task AddScore(string userId, int points)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return;

            user.Score += points;
            await _userManager.UpdateAsync(user);
        }

        private static object? PlayerInfo(Player? player)
        {
            if (player is null) return null;

            return new
            {
                player.Id,
                player.Name,
                player.Avatar,
                Guesses = player.Guesses.Count
            };
        }

        private static object BuildGameOver(GameSession session, bool restored = false)
        {
            return new
            {
                WinnerName = session.Winner?.Name,
                WinnerId = session.Winner?.Id,
                WinnerGuesses = session.Winner?.Guesses.Count ?? 0,
                Points = session.WinnerPoints,
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
            await Clients.Group(roomCode).SendAsync("ChatMessage", player.Id, player.Name, text, player.Avatar);            
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _queue.Remove(Context.ConnectionId);

            var session = _registry.RemoveConnection(Context.ConnectionId, out var roomCode, out var playerName);
            if (session != null && roomCode != null)
            {
                await Clients.Group(roomCode).SendAsync("PlayerLeft", playerName);
            }
            await base.OnDisconnectedAsync(exception);
        }

        private static GuessResult ScoreGuess(string guess, string target)
        {
            target = target.ToLower();
            var letters = new LetterState[5];
            var targetChars = target.ToCharArray();
            var used = new bool[5];

            for (int i = 0; i < 5; i++)
            {
                if (guess[i] == target[i])
                {
                    letters[i] = LetterState.Correct;
                    used[i] = true;
                }
            }
            
            for (int i = 0; i < 5; i++)
            {
                if (letters[i] == LetterState.Correct) continue;
                var found = false;
                for (int t = 0; t < 5; t++)
                {
                    if (!used[t] && guess[i] == targetChars[t])
                    {
                        used[t] = true;
                        found = true;
                        break;
                    }
                }
                letters[i] = found ? LetterState.Misplaced : LetterState.Absent;
            }
            
            return new GuessResult
            {
                Guess = guess,
                Letters = letters,
                IsWin = guess == target
            };
        }
    }
}