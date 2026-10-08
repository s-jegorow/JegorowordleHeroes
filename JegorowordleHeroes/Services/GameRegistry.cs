using JegoroWordleHeroes.Models;
using System.Collections.Concurrent;

namespace JegoroWordleHeroes.Services
{
    public class GameRegistry
    {
        //concurrentdict -> threadsicher
        private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

        private readonly Timer _timer;

        public GameRegistry()
        {
            //aktivität prüfen, + aufräumen
            _timer = new Timer(_ => Cleanup(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private void Cleanup()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            foreach (var kv in _sessions)
            {
                if (kv.Value.LastActivity < cutoff)
                {
                    _sessions.TryRemove(kv.Key, out _);
                }
            }
        }

        public GameSession GetOrCreate(string roomCode, string targetWord)
            => _sessions.GetOrAdd(roomCode, _ => new GameSession(roomCode, targetWord));

        public GameSession StartNew(string roomCode, string targetWord)
        {
            var session = new GameSession(roomCode, targetWord);
            _sessions[roomCode] = session;
            return session;
        }

        public GameSession? Get(string roomCode)
            => _sessions.TryGetValue(roomCode, out var s) ? s : null;

        public GameSession? RemoveConnection(string connectionId, out string? roomCode, out string? playerName)
        {
            foreach (var kv in _sessions)
            {
                var s = kv.Value;
                bool isA = s.PlayerA?.ConnectionId == connectionId;
                bool isB = s.PlayerB?.ConnectionId == connectionId;

                if (isA || isB)
                {
                    var player = isA ? s.PlayerA! : s.PlayerB!;
                    player.ConnectionId = "";
                    playerName = player.Name;

                    roomCode = kv.Key;
                    return s;
                }
            }

            roomCode = null;
            playerName = null;
            return null;
        }
    }
}
