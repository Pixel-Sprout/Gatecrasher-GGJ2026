using Masquerade.Factories;
using Masquerade.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;

namespace Masquerade.Hubs;

public class MainHub(ILogger<MainHub> log, PlayerFactory playerFactory, GameFactory gameFactory):Hub
{
    private Player CurrentPlayer
    {
        get
        {
            if (Context.Items.ContainsKey("player"))
                return (Player) Context.Items["player"]!;
            throw new InvalidOperationException("Player not found in context");
        }
        set => Context.Items["player"] = value;
    }
    
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userToken = httpContext?.Request.Query["userToken"].ToString()!;
        var roomToJoin = StringValues.Empty;
        httpContext?.Request.Query.TryGetValue("roomToJoin", out roomToJoin);
        log.LogInformation("Client connected: {ConnectionId}, token {userToken}, room {roomToJoin}", Context.ConnectionId, userToken, roomToJoin);

        CurrentPlayer = playerFactory.GetOrCreate(userToken, Context.ConnectionId);
        if(!string.IsNullOrEmpty(CurrentPlayer.Username))
        {
            log.LogInformation("Player found for token {userToken}: {player}", userToken, CurrentPlayer);
            await SetPlayerName(CurrentPlayer.Username);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        
    }
    
    public async Task SetPlayerName(string name)
    {
        if(string.IsNullOrEmpty(name.Trim())) return;
        
        log.LogInformation("Client {player} set name to {Name}", CurrentPlayer, name);
        CurrentPlayer.Username = name;

        if (!string.IsNullOrEmpty(CurrentPlayer.lastAttachedGameId) && 
                !gameFactory.DoGameExist(CurrentPlayer.lastAttachedGameId))
        {
            CurrentPlayer.lastAttachedGameId = null;
        }
        
        await Clients.Caller.SendAsync("playerDataReceived", name, CurrentPlayer.lastAttachedGameId, gameFactory.GetAllGames());
    }

    
    
    public async Task CreateRoom(string roomName)
    {
        log.LogInformation("Client {player} created room {roomName}", CurrentPlayer, roomName);
        
        IUiGame room = gameFactory.Create(roomName);
        await Clients.All.SendAsync("roomCreated", room as IUiGame);
    }
}