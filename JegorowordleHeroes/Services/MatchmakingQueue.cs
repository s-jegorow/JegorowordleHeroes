namespace JegoroWordleHeroes.Services
{
    public record WaitingPlayer(string ConnectionId, string PlayerId);

    public class MatchmakingQueue
    {
        private readonly object _lock = new();
        private WaitingPlayer? _waiting;

        public WaitingPlayer? TryMatch(WaitingPlayer player)
        {
            lock (_lock)
            {
                if (_waiting == null || _waiting.PlayerId == player.PlayerId)
                {
                    _waiting = player;
                    return null;
                }

                var opponent = _waiting;
                _waiting = null;
                return opponent;
            }
        }

        public void Remove(string connectionId)
        {
            lock (_lock)
            {
                if (_waiting?.ConnectionId == connectionId)
                {
                    _waiting = null;
                }
            }
        }
    }
}
