namespace JegoroWordleHeroes.Models
{
    public class GuessResult
    {
        public string Guess { get; set; } = "";
        public LetterState[] Letters { get; set; } = new LetterState[5];
        public bool IsWin { get; set; }
    }
}
