using JegoroWordleHeroes.Models;

namespace JegoroWordleHeroes.Tests
{
    public class ScoreTests
    {
        private const LetterState A = LetterState.Absent;
        private const LetterState M = LetterState.Misplaced;
        private const LetterState C = LetterState.Correct;

        [Theory]
        [InlineData("stern", "stern", new[] { C, C, C, C, C })]
        [InlineData("apfel", "kugel", new[] { A, A, A, C, C })]
        [InlineData("nacht", "tanne", new[] { M, C, M, A, A })]
        [InlineData("radio", "kanal", new[] { A, C, A, A, A })]
        [InlineData("kater", "taten", new[] { A, C, C, C, A })]
        public void Score_MarksLetters(string target, string guess, LetterState[] expected)
        {
            var result = GameSession.Score(guess, target);

            Assert.Equal(expected, result.Letters);
        }

        [Fact]
        public void Score_IgnoresCaseOfTarget()
        {
            Assert.True(GameSession.Score("kater", "Kater").IsWin);
        }

        [Fact]
        public void Score_NoWinOnPartialMatch()
        {
            Assert.False(GameSession.Score("kater", "katze").IsWin);
        }
    }
}
