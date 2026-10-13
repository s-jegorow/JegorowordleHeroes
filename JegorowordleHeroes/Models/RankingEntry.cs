namespace JegoroWordleHeroes.Models
{
    public class RankingEntry
    {
        public int Rank { get; set; }
        public string UserName { get; set; } = "";
        public int Score { get; set; }
        public int Avatar { get; set; }
        public bool IsMe { get; set; }
    }
}
