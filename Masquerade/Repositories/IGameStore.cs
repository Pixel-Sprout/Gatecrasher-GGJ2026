using Masquerade.Models;

namespace Masquerade.Repositories
{
    public interface IGameStore
    {
        Game? Get(string? gameId);
        IEnumerable<Game> GetAllGames();
        Game Create(string? gameName);
        bool Remove(string gameId);
    }
}
