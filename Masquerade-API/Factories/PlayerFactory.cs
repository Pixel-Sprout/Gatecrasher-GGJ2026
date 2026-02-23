using Masquerade.Models;
using Masquerade.Notifiers;

namespace Masquerade.Factories
{
    public class PlayerFactory
    {
        private readonly ILogger<PlayerFactory> _log;
        private readonly PlayerNotifier _notifier;

        public PlayerFactory(ILogger<PlayerFactory> log, PlayerNotifier notifier)
        {
            _log = log;
            _notifier = notifier;
        }

        // Create a new Game and attach a per-game notifier instance
        public Player Create(string userToken, string connectionId, string? userName = null)
        {
            var player = new Player(userToken, connectionId, userName, _notifier);
            _log.LogInformation("Created new player with ID: {PlayerId}", player.UserId);
            return player;
        }
    }
}