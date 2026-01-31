namespace Masquerade_GGJ_2026.Hubs
{
    using Masquerade_GGJ_2026.Models;
    using Masquerade_GGJ_2026.Models.Messages;
    using Masquerade_GGJ_2026.Orchestrators;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class GameHub(ILogger<GameHub> log) : Hub
    {
        static List<Game> Games = new() { new Game() };

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var username = httpContext?.Request.Query["username"].ToString();
            if (!string.IsNullOrEmpty(username))
            {
                Context.Items["username"] = username;
                log.LogInformation("User connected to GameHub: {Username}, ConnectionId: {ConnectionId}", username, Context.ConnectionId);
            }

            await Clients.All.SendAsync("UserJoinedGame", Context.ConnectionId, username);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            log.LogInformation("User disconnected from GameHub: {Username}, ConnectionId: {ConnectionId}", username, Context.ConnectionId);

            // Global notification to all connected clients (existing behavior)
            await Clients.All.SendAsync("UserLeftGame", Context.ConnectionId, username);

            // If the connection was in a game group, notify that group and remove the connection
            if (Context.Items.ContainsKey("gameId"))
            {
                var gameId = Context.Items["gameId"]?.ToString();
                if (!string.IsNullOrEmpty(gameId))
                {
                    await Clients.Group(gameId).SendAsync("UserLeftGameGroup", Context.ConnectionId, username, gameId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
                    log.LogInformation("Connection {ConnectionId} left game group {GameId}", Context.ConnectionId, gameId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Allows a connected client to join a specific game by id.
        /// Adds the connection to the group, stores the gameId in Context.Items,
        /// and broadcasts the join to members of that game only.
        /// </summary>
        public async Task JoinGame(string gameId)
        {
            //Already in a different game
            if(Context.Items["gameId"] == null)
            {
                return;
            }

            //Invalid gameId
            if (Guid.TryParse(gameId, out Guid gameIdGuid))
            {
                return;
            }

            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            Context.Items["gameId"] = gameIdGuid;
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            log.LogInformation("Connection {ConnectionId} joined game group {GameId} via JoinGame", Context.ConnectionId, gameIdGuid);

            await Clients.Group(gameId).SendAsync("UserJoinedGameGroup", Context.ConnectionId, username, gameIdGuid);

            // Send current game state to the caller if available
            var game = Games.FirstOrDefault(g => g.GameId == gameIdGuid);
            if (game != null)
            {
                game.Players.Add(new Player { ConnectionId = Context.ConnectionId, Username = username });
            }
        }

        /// <summary>
        /// Allows a connected client to leave a specific game group.
        /// </summary>
        public async Task LeaveGame()
        {
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            if (Guid.TryParse(gameId, out Guid gameIdGuid))
            {
                return;
            }

            Context.Items.Remove("gameId");

            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;

            await Clients.Group(gameId!).SendAsync("UserLeftGameGroup", Context.ConnectionId, username, gameId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId!);

            log.LogInformation("Connection {ConnectionId} left game group {GameId} via LeaveGame", Context.ConnectionId, gameId);

            // Send current game state to the caller if available
            var game = Games.FirstOrDefault(g => g.GameId == gameIdGuid);
            if (game != null)
            {
                game.Players.RemoveAll(p => p.ConnectionId == Context.ConnectionId);
            }
        }

        public async Task DrawingReady(string encodedDrawing)
        {
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            if (!Guid.TryParse(gameId, out Guid gameIdGuid))
            {
                return;
            }
            var game = Games.FirstOrDefault(g => g.GameId == gameIdGuid);
            if (game != null)
            {
                var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    player.EncodedMask = encodedDrawing;
                    log.LogInformation("Player {Username} in Game {GameId} submitted drawing", player.Username, gameIdGuid);
                    // Check if all players have submitted their drawings
                    if (game.Players.All(p => !string.IsNullOrEmpty(p.EncodedMask)))
                    {
                        log.LogInformation("All players in Game {GameId} have submitted drawings. Advancing phase.", gameIdGuid);
                        await PhaseChanged(gameIdGuid);
                    }
                }
            }
        }

        public async Task PhaseChanged(Guid gameId)
        {
            var game = Games.FirstOrDefault(g => g.GameId == gameId);
            if (game != null)
            {
                var newPhase = (int)(game.CurrentPhase + 1) % 4;
                game.CurrentPhase = (RoundPhase)newPhase;

                log.LogInformation("Broadcasting PhaseChanged for GameId {GameId} to group", gameId);
                
                switch (game.CurrentPhase)
                {
                    case RoundPhase.Lobby:
                        await Clients.Group(gameId.ToString()).SendAsync("PhaseChanged", game.CurrentPhase, GameOrchestrator.CreateLobbyMessage(game));
                        break;
                    case RoundPhase.Drawing:
                        var badPlayer = game.Players[new Random().Next(game.Players.Count)];
                        foreach(var player in game.Players)
                        {
                            player.IsEvil = player == badPlayer;
                            await Clients.Group(gameId.ToString()).SendAsync("PhaseChanged", game.CurrentPhase, GameOrchestrator.CreateDrawingMessage(game, player.IsEvil));
                        }
                        break;
                    case RoundPhase.Voting:
                        await Clients.Group(gameId.ToString()).SendAsync("PhaseChanged", game.CurrentPhase, GameOrchestrator.CreateVotingMessage(game));
                        break;
                    case RoundPhase.Scoreboard:
                        await Clients.Group(gameId.ToString()).SendAsync("PhaseChanged", game.CurrentPhase, GameOrchestrator.CreateScoreboardMessage(game));
                        break;
                    default:
                        throw new ArgumentException("Invalid game phase");
                };

            }
        }
    }
}