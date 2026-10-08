namespace JegoroWordleHeroes.Models
{
    public class GameSession
    {
        public string RoomCode { get; }
        public string TargetWord { get; }
        public bool IsOver { get; set; }
        public string? WinnerName { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public Player? PlayerA { get; private set; }
        public Player? PlayerB { get; private set; }

        public GameSession(string roomCode, string targetWord)
        {
            RoomCode = roomCode;
            TargetWord = targetWord;
        }

        public string TargetWordMask => "*****";

        public bool IsReady => PlayerA != null && PlayerB != null;

        public Player? AddOrReconnectPlayer(string connectionId, string playerId, string name)
        {
            if (PlayerA == null || PlayerA.Id == playerId)
            {
                PlayerA ??= new Player { Id = playerId, Name = name };
                PlayerA.ConnectionId = connectionId;
                return PlayerA;
            }
            if (PlayerB == null || PlayerB.Id == playerId)
            {
                PlayerB ??= new Player { Id = playerId, Name = name };
                PlayerB.ConnectionId = connectionId;
                return PlayerB;
            }
            return null;
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

    }
}
