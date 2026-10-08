using System.Collections.Generic;

namespace JegoroWordleHeroes.Models
{
    public class Player
    {
        public string Id { get; set; } = "";
        public string ConnectionId { get; set; } = "";
        public string Name { get; set; } = "";
        public List<GuessResult> Guesses { get; } = new();
    }
}
