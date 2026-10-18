using System.Collections.Generic;

namespace JegoroWordleHeroes.Models
{
    public class Player
    {
        public string Id { get; set; } = "";
        public string PublicId { get; } = Guid.NewGuid().ToString("N");
        public string ConnectionId { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsRegistered { get; set; }
        public int Avatar { get; set; }
        public List<GuessResult> Guesses { get; } = new();

    }
}
