using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Messages;
using Masquerade_GGJ_2026.Notifiers;
using Masquerade_GGJ_2026.Repositories;

namespace Masquerade_GGJ_2026.Factories;

public class GameRoomOrchestrator(IGameStore gameStore, 
    GameNotifier gameNotifier,
    RoomSelectionNotifier roomSelectionNotifier)
{
    public Game? GetGame(string? gameId)
        => gameStore.Get(gameId) ?? null;

    public Game Create(string gameName)
    {
        var newGame = new Game(string.IsNullOrEmpty(gameName) ? Guid.NewGuid().ToString() : gameName, gameNotifier);
        return gameStore.Create(newGame);
    }

    public async Task PlayerJoinGame(Game game, Player player, bool reconnect = false)
    {
        await roomSelectionNotifier.UserLeft(player);
        await game.NotifyUserJoined(player);
        if (reconnect)
        {
            await player.NotifyPlayerState();
        }
        await player.NotifyPhaseChanged(game);
        await game.NotifySendPlayersInRoom();
    }
    

    private async Task NotifyplayerAboutRooms(Player player)
    {
        GameRoomMessage[] gameRooms =gameStore.GetAllGames()
            .Select(g => new GameRoomMessage()
            {
                GameId = g.GameId,
                GameName = g.GameName,
                CurrentPhase = g.PhaseDetails.CurrentPhase.ToString()
            }).ToArray();;
        await player.NotifyAllGameRooms(gameRooms);
    }
    
    public bool Remove(string gameId)
        => gameStore.Remove(gameId);
    
}