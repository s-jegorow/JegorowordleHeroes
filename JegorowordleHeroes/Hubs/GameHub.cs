using JegoroWordleHeroes.Models;
using JegoroWordleHeroes.Services;
using Microsoft.AspNetCore.SignalR;

namespace JegoroWordleHeroes.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameRegistry _registry;
        private readonly WordService _words;

        public GameHub(GameRegistry registry, WordService words)
        {
            _registry = registry;
            _words = words;
        }

        public async Task<string> CreateOrJoin(string roomCode, string playerId, string playerName)
        {
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

            var player = session.AddOrReconnectPlayer(Context.ConnectionId, playerId, playerName);

            if (player is null)
            {
                await Clients.Caller.SendAsync("RoomFull");
                return string.Empty;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("PlayersUpdated",
                session.PlayerA?.Name, session.PlayerB?.Name);

            if (session.IsReady && !session.IsOver)
            {
                await Clients.Group(roomCode).SendAsync("GameReady", 6, 5);
            }
            return session.TargetWordMask;
        }

        public async Task SubmitGuess(string roomCode, string guess)
        {
            var session = _registry.Get(roomCode);
            if (session is null) return;
            session.LastActivity = DateTime.UtcNow;

            var player = session.FindByConnection(Context.ConnectionId);
            if (player is null || session.IsOver) return;

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
                         .SendAsync("OpponentGuessed", player.Name, player.Guesses.Count);

            if (result.IsWin)
            {
                session.WinnerName = player.Name;
                session.IsOver = true;
                await Clients.Group(roomCode).SendAsync("GameOver", new {
                    WinnerName = player.Name,
                    WinnerId = player.Id,
                    ByWin = true,
                    TargetWord = session.TargetWord,
                    PlayerAGuesses = session.PlayerA?.Guesses.Count ?? 0,
                    PlayerBGuesses = session.PlayerB?.Guesses.Count ?? 0,
                    PlayerAName = session.PlayerA?.Name,
                    PlayerBName = session.PlayerB?.Name
                });
                return;
            }

            if (session.AllPlayersExhausted(6))
            {
                session.IsOver = true;
                await Clients.Group(roomCode).SendAsync("GameOver", new {
                    WinnerName = session.WinnerName,
                    ByWin = false,
                    TargetWord = session.TargetWord,
                    PlayerAGuesses = session.PlayerA?.Guesses.Count ?? 0,
                    PlayerBGuesses = session.PlayerB?.Guesses.Count ?? 0,
                    PlayerAName = session.PlayerA?.Name,
                    PlayerBName = session.PlayerB?.Name
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
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