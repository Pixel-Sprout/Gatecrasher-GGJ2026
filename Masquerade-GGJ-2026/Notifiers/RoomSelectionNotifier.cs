using Masquerade_GGJ_2026.Hubs;
using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Messages;
using Masquerade_GGJ_2026.Factories;
using Microsoft.AspNetCore.SignalR;

namespace Masquerade_GGJ_2026.Notifiers;

public class RoomSelectionNotifier(IHubContext<GameHub> hub)
{
    public async Task UserJoined(Player player)
    {
        await hub.Groups.AddToGroupAsync(player.ConnectionId, groupName: String.Empty);
    }

    public async Task UserLeft(Player player)
    {
        await hub.Groups.RemoveFromGroupAsync(player.ConnectionId, groupName: String.Empty);
    }
    
    
}