using System.Collections.Generic;

namespace JegoroWordleHeroes.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } = "";
        public string Name { get; set; } = "";
        public List<GuessResult> Guesses { get; } = new();
    }
}
