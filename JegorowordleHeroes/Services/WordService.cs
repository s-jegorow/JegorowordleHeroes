using System;
using System.Collections.Generic;
using System.Linq;

namespace JegoroWordleHeroes.Services
{
    public class WordService
    {
        // einfache Demo-Liste (de/en gemischt, 5 Buchstaben)
        private static readonly string[] Words = new[]
        {
            "kater","farbe","holzs","steak","nacht","laser","eimer","taste","zebra",
            "apfel","stern","radio","stuhl","feder","kabel","banjo","kugel","wolke",
            "vogel","honig","piano","pixel","krona","gamma","delta","omega","angel"
        };

        private readonly HashSet<string> _dict;

        public WordService()
        {
            _dict = new HashSet<string>(Words.Select(w => w.ToLowerInvariant()));
        }

        public string PickWord()
        {
            var rnd = Random.Shared.Next(Words.Length);
            return Words[rnd];
        }

        public bool IsValid(string word) => _dict.Contains((word ?? "").ToLowerInvariant());
    }
}
