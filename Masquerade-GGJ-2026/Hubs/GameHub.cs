using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Orchestrators;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Masquerade_GGJ_2026.Hubs
{
    public class GameHub : Hub
    {
        private readonly ILogger<GameHub> _log;
        private readonly GameNotifier _notifier;
        private readonly GameOrchestrator _orchestrator;

        public GameHub(ILogger<GameHub> log, GameNotifier notifier, GameOrchestrator orchestrator)
        {
            _log = log;
            _notifier = notifier;
            _orchestrator = orchestrator;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var username = httpContext?.Request.Query["username"].ToString();
            if (!string.IsNullOrEmpty(username))
            {
                Context.Items["username"] = username;
                _log.LogInformation("User connected to GameHub: {Username}, ConnectionId: {ConnectionId}", username, Context.ConnectionId);
            }

            //await Clients.All.SendAsync("UserJoinedGame", Context.ConnectionId, username);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            _log.LogInformation("User disconnected from GameHub: {Username}, ConnectionId: {ConnectionId}", username, Context.ConnectionId);

            // Global notification to all connected clients (existing behavior)
            //await Clients.All.SendAsync("UserLeftGame", Context.ConnectionId, username);

            // If the connection was in a game group, notify that group and remove the connection
            if (Context.Items.ContainsKey("gameId"))
            {
                var gameId = Context.Items["gameId"]?.ToString();
                if (!string.IsNullOrEmpty(gameId))
                {
                    await _notifier.UserLeft(gameId, Context.ConnectionId, username);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Broadcast all game ids to the connected clients
        public async Task GetAllGameIds()
        {
            var gameIds = GamesState.Games.Select(g => g.GameId.ToString()).ToList();
            await Clients.Caller.SendAsync("ReceiveAllGameIds", gameIds);
        }

        /// <summary>
        /// Allows a connected client to join a specific game by id.
        /// Adds the connection to the group, stores the gameId in Context.Items,
        /// and broadcasts the join to members of that game only.
        /// </summary>
        public async Task JoinGame(string gameId)
        {
            //Already in a different game
            if (Context.Items["gameId"] != null)
            {
                await Clients.Caller.SendAsync("Error", "Already in a game. Leave current game before joining another.");
                return;
            }

            //Invalid gameId
            if (!Guid.TryParse(gameId, out Guid gameIdGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid game ID.");
                return;
            }

            var game = GamesState.Games.FirstOrDefault(g => g.GameId == gameIdGuid);
            if (game == null || game.PhaseDetails.CurrentPhase != RoundPhase.Lobby)
            {
                await Clients.Caller.SendAsync("Error", "Game not found or already started.");
                return;
            }

            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            Context.Items["gameId"] = gameIdGuid;

            await _notifier.UserJoined(gameId!, Context.ConnectionId, username);

            game.Players.Add(new Player { ConnectionId = Context.ConnectionId, Username = username });
            await Clients.Caller.SendAsync("PlayersInTheRoom", game.Players.Select(p => p.Username).ToList());
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
            await _notifier.UserLeft(gameId!, Context.ConnectionId, username);

            // Send current game state to the caller if available
            var game = GamesState.Games.FirstOrDefault(g => g.GameId == gameIdGuid);
            if (game != null)
            {
                game.Players.RemoveAll(p => p.ConnectionId == Context.ConnectionId);
            }
        }

        public async Task PlayerReady()
        {
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            if (!Guid.TryParse(gameId, out Guid gameIdGuid))
            {
                return;
            }
            var game = GamesState.Games.FirstOrDefault(g => g.GameId == gameIdGuid);
            if (game != null)
            {
                var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    player.IsReady = !player.IsReady;
                    await _notifier.PlayerReady(game, player);
                    _log.LogInformation("Player {Username} in Game {GameId} is ready", player.Username, gameIdGuid);
                    // Check if all players are ready
                    if (game.Players.All(p => p.IsReady))
                    {
                        _log.LogInformation("All players in Game {GameId} are ready. Advancing phase.", gameIdGuid);
                        await _orchestrator.EndPhase(game, "All players ready");
                    }
                }
            }
        }

        public async Task CastVote(string selectedPlayerId)
        {
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            if (!Guid.TryParse(gameId, out Guid gameIdGuid))
            {
                return;
            }
            var game = GamesState.Games.FirstOrDefault(g => g.GameId == gameIdGuid);
            if (game != null)
            {
                var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    player.VotedPlayerId = selectedPlayerId;
                    _log.LogInformation("Player {Username} in Game {GameId} casted vote", player.Username, gameIdGuid);
                    // Check if all players have submitted their drawings
                }
            }
        }

    }
}