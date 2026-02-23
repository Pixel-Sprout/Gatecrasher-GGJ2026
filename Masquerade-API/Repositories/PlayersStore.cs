using System.Collections.Concurrent;
using Masquerade.Models;

namespace Masquerade.Repositories;

public interface IPlayersStore
{
    Player? GetPlayerByToken(string token);
    void NewPlayer(Player player);
}

internal class PlayersStore: IPlayersStore
{
    private readonly ConcurrentDictionary<string, Player> _players = new();

    public Player? GetPlayerByToken(string token)
    => _players.ContainsKey(token) ? _players[token] : null;
    

    public void NewPlayer(Player player)
    {
        _players.GetOrAdd(player.UserToken, _ => player);
    }
}