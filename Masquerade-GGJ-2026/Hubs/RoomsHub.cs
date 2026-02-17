using Microsoft.AspNetCore.SignalR;

namespace Masquerade_GGJ_2026.Hubs;

public class RoomsHub: Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var username = httpContext?.Request.Query["username"].ToString();
        var userToken = httpContext?.Request.Query["userToken"].ToString();
    }
}