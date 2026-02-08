using Masquerade_GGJ_2026.Models;

namespace Masquerade_GGJ_2026.Repositories
{
    public interface IGameStore
    {
        Game? Get(string? gameId);
        IEnumerable<Game> GetAllGames();
        Game Create(string? gameName);
        bool Remove(string gameId);
    }
}
