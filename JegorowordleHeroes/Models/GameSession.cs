namespace JegoroWordleHeroes.Models
{
    public enum RematchResult { Ignored, Requested, Started }

    public record GuessOutcome(GuessResult Result, int GuessCount, bool EndedGame);

    public class GameSession
    {
        public const int MaxGuesses = 6;

        public string RoomCode { get; }
        public bool IsSolo { get; }
        public string TargetWord { get; private set; }
        public bool IsOver { get; private set; }
        public Player? Winner { get; private set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        private readonly HashSet<string> _rematchVotes = new();
        // beide spieler können gleichzeitig raten oder revanche drücken, darum alles über diesen lock
        private readonly object _lock = new();

        public Player? PlayerA { get; private set; }
        public Player? PlayerB { get; private set; }

        public GameSession(string roomCode, string targetWord, bool isSolo = false)
        {
            RoomCode = roomCode;
            TargetWord = targetWord;
            IsSolo = isSolo;
        }

        public int WinnerPoints => Winner == null ? 0 : 700 - 100 * Winner.Guesses.Count;

        public bool IsReady => PlayerA != null && (IsSolo || PlayerB != null);

        public bool HasConnectedPlayers =>
            (PlayerA?.ConnectionId ?? "") != "" || (PlayerB?.ConnectionId ?? "") != "";

        public bool HasStarted => (PlayerA?.Guesses.Count ?? 0) + (PlayerB?.Guesses.Count ?? 0) > 0;

        public Player? AddOrReconnectPlayer(string connectionId, string playerId, string name, bool isRegistered)
        {
            lock (_lock)
            {
                if (IsSolo && PlayerA != null && PlayerA.Id != playerId) return null;

                if (PlayerA == null || PlayerA.Id == playerId)
                {
                    PlayerA ??= new Player { Id = playerId, Name = name, IsRegistered = isRegistered };
                    PlayerA.ConnectionId = connectionId;
                    return PlayerA;
                }
                if (PlayerB == null || PlayerB.Id == playerId)
                {
                    PlayerB ??= new Player { Id = playerId, Name = name, IsRegistered = isRegistered };
                    PlayerB.ConnectionId = connectionId;
                    return PlayerB;
                }
                if (!HasStarted && PlayerA.ConnectionId == "")
                {
                    PlayerA = new Player { Id = playerId, Name = name, IsRegistered = isRegistered, ConnectionId = connectionId };
                    return PlayerA;
                }
                if (!HasStarted && PlayerB.ConnectionId == "")
                {
                    PlayerB = new Player { Id = playerId, Name = name, IsRegistered = isRegistered, ConnectionId = connectionId };
                    return PlayerB;
                }
                return null;
            }
        }

        public GuessOutcome? TryGuess(Player player, string guess)
        {
            lock (_lock)
            {
                if (IsOver || player.Guesses.Count >= MaxGuesses) return null;

                var result = Score(guess, TargetWord);
                player.Guesses.Add(result);

                if (result.IsWin)
                {
                    Winner = player;
                    IsOver = true;
                }
                else if (AllPlayersExhausted())
                {
                    IsOver = true;
                }

                return new GuessOutcome(result, player.Guesses.Count, IsOver);
            }
        }

        public RematchResult VoteRematch(Player player, string newWord)
        {
            lock (_lock)
            {
                if (!IsOver) return RematchResult.Ignored;

                if (!IsSolo)
                {
                    _rematchVotes.Add(player.Id);
                    var opponent = GetOpponent(player);
                    if (opponent is null || !_rematchVotes.Contains(opponent.Id)) return RematchResult.Requested;
                }

                Reset(newWord);
                return RematchResult.Started;
            }
        }

        public bool HasPlayer(string playerId)
            => PlayerA?.Id == playerId || PlayerB?.Id == playerId;

        public Player? FindByConnection(string connectionId)
            => (PlayerA?.ConnectionId == connectionId) ? PlayerA
             : (PlayerB?.ConnectionId == connectionId) ? PlayerB
             : null;

        public Player? GetOpponent(Player player)
            => player == PlayerA ? PlayerB : PlayerA;

        public static GuessResult Score(string guess, string target)
        {
            target = target.ToLower();
            var letters = new LetterState[5];
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
                    if (!used[t] && guess[i] == target[t])
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

        private bool AllPlayersExhausted()
        {
            var a = PlayerA != null && PlayerA.Guesses.Count >= MaxGuesses;
            var b = PlayerB == null || PlayerB.Guesses.Count >= MaxGuesses;
            return a && b;
        }

        private void Reset(string newWord)
        {
            TargetWord = newWord;
            IsOver = false;
            Winner = null;
            PlayerA?.Guesses.Clear();
            PlayerB?.Guesses.Clear();
            _rematchVotes.Clear();
            LastActivity = DateTime.UtcNow;
        }
    }
}
