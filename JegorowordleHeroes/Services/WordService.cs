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
            string root = env.WebRootPath ?? env.ContentRootPath;

            _words = LoadWords(Path.Combine(root, "words.txt"));
            if (_words.Length == 0)
                _words = FallbackWords;

            _dict = new HashSet<string>(_words);
            _dict.UnionWith(LoadWords(Path.Combine(root, "allowed.txt")));
        }

        private static string[] LoadWords(string path)
        {
            string[] lines;
            try
            {
                lines = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();
            }
            catch
            {
                lines = Array.Empty<string>();
            }

            return lines
                .Select(l => l.Trim().ToLower())
                .Where(l => l.Length == 5)
                .Distinct()
                .ToArray();
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
