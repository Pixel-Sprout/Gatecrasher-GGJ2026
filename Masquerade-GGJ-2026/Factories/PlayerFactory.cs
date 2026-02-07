using Masquerade_GGJ_2026.Hubs;
using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Notifiers;
using Microsoft.AspNetCore.SignalR;

namespace Masquerade_GGJ_2026.Orchestrators
{
    public class PlayerFactory
    {
        private readonly ILogger<GameHub> _log;
        private readonly PlayerNotifier _notifier;

        public PlayerFactory(ILogger<GameHub> log, PlayerNotifier notifier)
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