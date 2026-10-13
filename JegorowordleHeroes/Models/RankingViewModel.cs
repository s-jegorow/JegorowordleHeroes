namespace JegoroWordleHeroes.Models
{
    public class RankingViewModel
    {
        public List<RankingEntry> Top { get; } = new();
        public RankingEntry? Me { get; set; }
    }
}
