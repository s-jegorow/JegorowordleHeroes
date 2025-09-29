using JegoroWordleHeroes.Models;
using JegoroWordleHeroes.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
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

        public async Task<string> CreateOrJoin(string roomCode, string playerName)
        {
            var session = _registry.GetOrCreate(roomCode, _words.PickWord());
            var player = session.AddOrReconnectPlayer(Context.ConnectionId, playerName);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("PlayersUpdated",
                session.PlayerA?.Name, session.PlayerB?.Name);

            if (session.IsReady)
            {
                await Clients.Group(roomCode).SendAsync("GameReady", 6, 5);
            }
            return session.TargetWordMask; // nie das echte Wort zurückgeben
        }

        public async Task SubmitGuess(string roomCode, string guess)
        {
            var session = _registry.Get(roomCode);
            if (session is null) return;

            var player = session.FindByConnection(Context.ConnectionId);
            if (player is null || session.IsOver) return;

            guess = (guess ?? "").Trim().ToLowerInvariant();

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
                         .SendAsync("OpponentGuessed", player.Name, result);

            if (result.IsWin)
            {
                session.WinnerName = player.Name;
                session.IsOver = true;
                await Clients.Group(roomCode).SendAsync("GameOver", player.Name, true);
                return;
            }

            if (session.AllPlayersExhausted(6))
            {
                session.IsOver = true;
                await Clients.Group(roomCode).SendAsync("GameOver", session.WinnerName, false);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var session = _registry.RemoveConnection(Context.ConnectionId, out var roomCode);
            if (session != null && roomCode != null)
            {
                await Clients.Group(roomCode).SendAsync("PlayersUpdated",
                    session.PlayerA?.Name, session.PlayerB?.Name);
            }
            await base.OnDisconnectedAsync(exception);
        }

        private static GuessResult ScoreGuess(string guess, string target)
        {
            var letters = new LetterState[5];
            var targetChars = target.ToCharArray();
            var used = new bool[5];

            // Treffer an Position (grün)
            for (int i = 0; i < 5; i++)
            {
                if (guess[i] == target[i])
                {
                    letters[i] = LetterState.Correct;
                    used[i] = true;
                }
            }
            // Falscher Platz (gelb) / fehlt (grau)
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
