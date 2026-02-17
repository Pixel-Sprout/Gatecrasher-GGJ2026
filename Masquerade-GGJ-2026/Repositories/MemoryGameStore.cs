using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Factories;
using System.Collections.Concurrent;

namespace Masquerade_GGJ_2026.Repositories
{
    internal class MemoryGameStore : IGameStore
    {
        private readonly ConcurrentDictionary<string, Game> _games = new();


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

        public Game Create(Game game)
        {
            return _games.GetOrAdd(game.GameId, _ => game);
        }

        public bool Remove(string gameId)
        {
            return _games.TryRemove(gameId, out _);
        }
    }
}
