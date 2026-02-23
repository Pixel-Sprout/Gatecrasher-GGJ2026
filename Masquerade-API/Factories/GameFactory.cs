using Masquerade.Models;
using Masquerade.Notifiers;
using Masquerade.Repositories;

namespace Masquerade.Factories
{
    public class GameFactory(ILogger<GameFactory> log, 
        GameNotifier notifier,
        IGameStore gameStore)
    {

        public IEnumerable<IUiGame> GetAllGames()
            => gameStore.GetAllGames().ToArray();
        public bool DoGameExist(string gameId)
            => gameStore.Get(gameId) != null;
        
        public Game Create(string? gameName = null)
        {
            var game = gameStore.Create(new Game(gameName, notifier));
            log.LogInformation("Created new game with ID: {GameId}", game.GameId);
            return game;
        }
    }
}