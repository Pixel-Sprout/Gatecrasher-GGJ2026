namespace Masquerade_GGJ_2026.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using System;
    using System.Threading.Tasks;

    public class EchoHub(ILogger<EchoHub> log) : Hub
    {
        public static IDictionary<string, string> clients = new Dictionary<string, string>();
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var username = httpContext?.Request.Query["username"].ToString();
            if (!string.IsNullOrEmpty(username))
            {
                Context.Items["username"] = username;
                clients.Add(Context.ConnectionId, username);
                log.LogInformation("User connected: {Username}, ConnectionId: {ConnectionId}", username, Context.ConnectionId);
                await Clients.Caller.SendAsync($"Aktywni użytkownicy: {string.Join(", ", clients.Values)}");
            }

            await Clients.All.SendAsync("UserJoined", Context.ConnectionId, username);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            log.LogInformation("User disconnected: {Username}, ConnectionId: {ConnectionId}", username, Context.ConnectionId);
            await Clients.All.SendAsync("UserLeft", Context.ConnectionId, username);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string message)
        {
            // odpowied� po 15 sekundach tylko do nadawcy
            log.LogInformation("Received message from {ConnectionId}: {Message}", Context.ConnectionId, message);
            await Task.Delay(15_000);
            await Clients.All.SendAsync("ReceiveMessage", message);
            log.LogInformation("Sent message from {ConnectionId}: {Message}", Context.ConnectionId, message);
        }
    }
}
