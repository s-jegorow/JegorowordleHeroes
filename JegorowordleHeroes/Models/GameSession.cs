namespace JegoroWordleHeroes.Models
{
    public class GameSession
    {
        public string RoomCode { get; }
        public string TargetWord { get; }
        public bool IsOver { get; set; }
        public string? WinnerName { get; set; }

        public Player? PlayerA { get; private set; }
        public Player? PlayerB { get; private set; }

        public GameSession(string roomCode, string targetWord)
        {
            RoomCode = roomCode;
            TargetWord = targetWord;
        }

        public string TargetWordMask => "*****";

        public bool IsReady => PlayerA != null && PlayerB != null;

        public Player AddOrReconnectPlayer(string connectionId, string name)
        {
            if (PlayerA == null || PlayerA.Name == name)
            {
                PlayerA ??= new Player { Name = name };
                PlayerA.ConnectionId = connectionId;
                return PlayerA;
            }
            if (PlayerB == null || PlayerB.Name == name)
            {
                PlayerB ??= new Player { Name = name };
                PlayerB.ConnectionId = connectionId;
                return PlayerB;
            }
            // Ersatz: überschreibe A bei drittem Join mit neuem Namen
            PlayerA = new Player { Name = name, ConnectionId = connectionId };
            return PlayerA;
        }

        public Player? FindByConnection(string connectionId)
            => (PlayerA?.ConnectionId == connectionId) ? PlayerA
             : (PlayerB?.ConnectionId == connectionId) ? PlayerB
             : null;

        public bool AllPlayersExhausted(int maxGuesses)
        {
            var a = PlayerA != null && PlayerA.Guesses.Count >= maxGuesses;
            var b = PlayerB != null && PlayerB.Guesses.Count >= maxGuesses;
            return a && b;
        }

    }
}
