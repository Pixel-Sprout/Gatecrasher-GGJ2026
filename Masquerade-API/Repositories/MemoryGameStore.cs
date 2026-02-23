using Masquerade.Orchestrators;
using System.Collections.Concurrent;
using Masquerade.Factories;
using Masquerade.Models;

namespace Masquerade.Repositories
{
    public class MemoryGameStore : IGameStore
    {
        private readonly ConcurrentDictionary<string, Game> _games = new();
        private readonly GameFactory _gameFactory;

        public MemoryGameStore(GameFactory gameFactory) 
        {
            _gameFactory = gameFactory;
        }

        public Game? Get(string? gameId)
        {
            if (string.IsNullOrEmpty(gameId) || !_games.ContainsKey(gameId))
            {
                return null;
            }
            return _games[gameId];
        }

        public IEnumerable<Game> GetAllGames()
        {
            return _games.Values;
        }

        public Game Create(string? gameName)
        {
            Game newGame = _gameFactory.Create(gameName);
            return _games.GetOrAdd(newGame.GameId, _ => newGame);
        }

        public bool Remove(string gameId)
        {
            return _games.TryRemove(gameId, out _);
        }
    }
}
