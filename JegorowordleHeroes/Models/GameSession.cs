namespace JegoroWordleHeroes.Models
{
    public class GameSession
    {
        public string RoomCode { get; }
        public string TargetWord { get; private set; }
        public bool IsOver { get; set; }
        public Player? Winner { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public HashSet<string> RematchVotes { get; } = new();
        private readonly object _playerLock = new();

        public Player? PlayerA { get; private set; }
        public Player? PlayerB { get; private set; }

        public GameSession(string roomCode, string targetWord)
        {
            RoomCode = roomCode;
            TargetWord = targetWord;
        }

        public int WinnerPoints => Winner == null ? 0 : 700 - 100 * Winner.Guesses.Count;

        public bool IsReady => PlayerA != null && PlayerB != null;

        public bool HasStarted => (PlayerA?.Guesses.Count ?? 0) + (PlayerB?.Guesses.Count ?? 0) > 0;

        public Player? AddOrReconnectPlayer(string connectionId, string playerId, string name, bool isRegistered)
        {
            lock (_playerLock)
            {
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

        public bool HasPlayer(string playerId)
            => PlayerA?.Id == playerId || PlayerB?.Id == playerId;

        public Player? FindByConnection(string connectionId)
            => (PlayerA?.ConnectionId == connectionId) ? PlayerA
             : (PlayerB?.ConnectionId == connectionId) ? PlayerB
             : null;

        public bool AllPlayersExhausted(int maxGuesses)
        {
            var a = PlayerA != null && PlayerA.Guesses.Count >= maxGuesses;
            var b = PlayerB == null || PlayerB.Guesses.Count >= maxGuesses;
            return a && b;
        }

        public Player? GetOpponent(Player player)
            => player == PlayerA ? PlayerB : PlayerA;

        public void Reset(string newWord)
        {
            TargetWord = newWord;
            IsOver = false;
            Winner = null;
            PlayerA?.Guesses.Clear();
            PlayerB?.Guesses.Clear();
            RematchVotes.Clear();
            LastActivity = DateTime.UtcNow;
        }

    }
}
