using Masquerade_GGJ_2026.Hubs;
using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Notifiers;

namespace Masquerade_GGJ_2026.Orchestrators
{
    public class GameFactory
    {
        private readonly ILogger<GameFactory> _log;
        private readonly GameNotifier _notifier;

        public GameFactory(ILogger<GameFactory> log, GameNotifier notifier)
        {
            _log = log;
            _notifier = notifier;
        }

        // Create a new Game and attach a per-game notifier instance
        public Game Create(string? gameName = null)
        {
            var game = new Game(gameName, _notifier);
            _log.LogInformation("Created new game with ID: {GameId}", game.GameId);
            return game;
        }
    }
}