using Masquerade.Models;

namespace Masquerade.Repositories
{
    public interface IGameStore
    {
        Game? Get(string? gameId);
        IEnumerable<Game> GetAllGames();
        Game Create(Game newGame);
        bool Remove(string gameId);
    }
}
