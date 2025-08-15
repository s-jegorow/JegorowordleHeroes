using System.Numerics;

namespace JegorowordleHeroes.Models
{
    public class JegorowordleGame
    {
        public string TargetWord { get; }
        public List<GuessResult> Guesses { get; } = new();
        public int MaxGuesses { get; }
        public bool IsFinished { get; private set; }
        public bool IsWon { get; private set; }

        public Player Player1 { get; }
        public Player Player2 { get; }

        public JegorowordleGame(string targetWord, Player player1, Player player2, int maxGuesses = 6)
        {
            TargetWord = targetWord;
            Player1 = player1;
            Player2 = player2;
            MaxGuesses = maxGuesses;
        }

        public GuessResult CheckGuess(string guess)
        {
            // Vergleichen, GuessResult erstellen, in Guesses hinzufügen,
            // IsWon/IsFinished setzen und result zurückgeben.
            // ggf. Methode aufteilen.
        }
    }
}
