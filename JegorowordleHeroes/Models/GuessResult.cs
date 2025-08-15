public class GuessResult
{
    public string Word { get; }
    public LetterResult[] LetterResults { get; }
    public bool IsWinningGuess { get; }

    public GuessResult(string word, LetterResult[] letterResults, bool isWinningGuess)
    {
        Word = word;
        LetterResults = letterResults;
        IsWinningGuess = isWinningGuess;
    }
}
