using JegoroWordleHeroes.Models;
using System.Collections.Concurrent;

namespace JegoroWordleHeroes.Services
{
    public class GameRegistry
    {
        private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

        public GameSession GetOrCreate(string roomCode, string targetWord)
            => _sessions.GetOrAdd(roomCode, _ => new GameSession(roomCode, targetWord));

        public GameSession? Get(string roomCode)
            => _sessions.TryGetValue(roomCode, out var s) ? s : null;

        public GameSession? RemoveConnection(string connectionId, out string? roomCode)
        {
            foreach (var kv in _sessions)
            {
                var s = kv.Value;
                bool isA = s.PlayerA?.ConnectionId == connectionId;
                bool isB = s.PlayerB?.ConnectionId == connectionId;

                if (isA || isB)
                {
                    if (isA) s.PlayerA!.ConnectionId = "";
                    if (isB) s.PlayerB!.ConnectionId = "";

                    roomCode = kv.Key;
                    return s;
                }
            }

            roomCode = null;
            return null;
        }
    }
}
