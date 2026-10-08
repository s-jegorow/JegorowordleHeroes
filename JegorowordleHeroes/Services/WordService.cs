using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace JegoroWordleHeroes.Services
{
    public class WordService
    {
        private static readonly string[] FallbackWords = new[]
        {
            "kater","farbe","nacht","apfel","stern","radio","stuhl","feder","kabel",
            "banjo","kugel","wolke","vogel","honig","piano","pixel","omega","angel"
        };

        private readonly string[] _words;
        private readonly HashSet<string> _dict;

        public WordService(IWebHostEnvironment env)
        {
            string path = Path.Combine(env.WebRootPath ?? env.ContentRootPath, "words.txt");

            string[] lines;
            try
            {
                lines = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();
            }
            catch
            {
                lines = Array.Empty<string>();
            }

            _words = lines
                .Select(l => l.Trim().ToLower())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Where(l => l.Length == 5)
                .Distinct()
                .ToArray();

            if (_words.Length == 0)
                _words = FallbackWords;

            _dict = new HashSet<string>(_words);
        }

        public string PickWord()
        {
            var i = Random.Shared.Next(_words.Length);
            return _words[i];
        }

        public bool IsValid(string word) =>
            !string.IsNullOrWhiteSpace(word) && _dict.Contains(word.ToLowerInvariant());
    }
}
