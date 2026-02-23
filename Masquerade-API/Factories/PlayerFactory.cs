using Masquerade.Models;
using Masquerade.Notifiers;
using Masquerade.Repositories;

namespace Masquerade.Factories
{
    public class PlayerFactory(ILogger<PlayerFactory> log, 
        PlayerNotifier notifier,
        IPlayersStore playersStore)
    {
        public Player GetOrCreate(string userToken, string connectionId, string? userName = null)
        {
            var player = playersStore.GetPlayerByToken(userToken);
            if (player == null)
            {
                player = new Player(userToken, connectionId, userName ?? String.Empty, notifier);
                playersStore.NewPlayer(player);
                log.LogInformation("Created new player with ID: {PlayerId}", player.UserId);
            }
            else
            {
                player.ConnectionId = connectionId;
            }

            return player;
        }
    }
}