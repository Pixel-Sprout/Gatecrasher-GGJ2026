using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Orchestrators;
using Microsoft.AspNetCore.SignalR;
using Masquerade_GGJ_2026.Models.Messages;

namespace Masquerade_GGJ_2026.Hubs
{
    public class GameHub : Hub
    {
        private readonly ILogger<GameHub> _log;
        private readonly GameNotifier _notifier;
        private readonly GameOrchestrator _orchestrator;
        private static IDictionary<string, Player> _players = new Dictionary<string, Player>();

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
            var userToken = httpContext?.Request.Query["userToken"].ToString();

            if (string.IsNullOrEmpty(userToken))
            {
                userToken = Guid.NewGuid().ToString();
            }

            if (!string.IsNullOrEmpty(username))
            {
                Player player;
                
                if (_players.TryGetValue(userToken, out player))
                {
                    player.ConnectionId = Context.ConnectionId;
                    player.IsRemoved = false;
                    if (player.lastAttachedGameId.HasValue)
                    {
                        Context.Items["gameId"] = player.lastAttachedGameId.Value.ToString();
                        Context.Items["username"] = player.Username;
                        Context.Items["player"] = player;
                        var game = GamesState.Games.FirstOrDefault(g => g.GameId == player.lastAttachedGameId.Value);
                        if (game != null)
                        {
                            var playerState= game.Players.FirstOrDefault(p => p.Player == player);
                            if (playerState == null)
                            {
                                game.Players.Add(new PlayerGameState { Player = player });
                            }
                            _notifier.UserJoined(player.lastAttachedGameId.Value, player);
                            await Clients.Caller.SendAsync("PlayerState", username, player.UserId);
                            await Clients.Caller.SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, _notifier.CreateLobbyMessage(game));
                            await _notifier.SendPlayersInRoom(game);
                        }
                    }
                }
                else
                {
                    player = new Player
                    {
                        ConnectionId = Context.ConnectionId,
                        Username = username,
                        UserToken = userToken
                    };
                    _players.Add(userToken, player);
                }

                player.IsRemoved = false;
                Context.Items["username"] = username;
                Context.Items["player"] = player;
                await Clients.Caller.SendAsync("PlayerState", username, player.UserId);
                _log.LogInformation("User connected to GameHub: {Username} [{UserToken}], ConnectionId: {ConnectionId}", username, userToken, Context.ConnectionId);
            }

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
                if (Guid.TryParse(gameId, out Guid gameIdGuid))
                {
                    var game = GamesState.Games.FirstOrDefault(x => x.GameId == gameIdGuid);
                    var player = (Player) Context.Items["player"];
                    if(game != null && player != null)
                    {
                        await detachPlayerFromGame(player, game);
                        await _notifier.UserLeft(gameId, player);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Broadcast all game ids to the connected clients
        public async Task GetAllGameIds()
        {
            await Clients.Caller.SendAsync("ReceiveAllGameIds", 
                GamesState.Games
                    .Select(g => new GameRoomMessage()
                    {
                        GameId =  g.GameId,
                        GameName = g.GameName,
                        CurrentPhase = g.PhaseDetails.CurrentPhase.ToString()
                    }).ToArray());
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

            var player = (Player) Context.Items["player"];
            await _notifier.UserJoined(gameIdGuid, player);

            game.Players.Add(new PlayerGameState { Player = player });

            await Clients.Caller.SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, _notifier.CreateLobbyMessage(game));
            await _notifier.SendPlayersInRoom(game);
        }

        public async Task<string> CreateAndJoinGame(string gameName)
        {
            //Create new game
            var newGame = new Game
            {
                GameName = string.IsNullOrWhiteSpace(gameName) ? $"Room {Guid.NewGuid()}" : gameName
            };
            GamesState.Games.Add(newGame);
            await _orchestrator.EndPhase(newGame, "New Game");
            //await GetAllGameIds();
            //Join the newly created game
            await JoinGame(newGame.GameId.ToString());

            await _notifier.PhaseChanged(newGame);

            return newGame.GameId.ToString();
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
            var player = (Player) Context.Items["player"];
            
            await _notifier.UserLeft(gameId!, player);
            var game = GamesState.Games.FirstOrDefault(g => g.GameId == gameIdGuid);
            if (game != null)
            {
                await detachPlayerFromGame(player, game);
            }
            
        }

        public async Task PlayerReady()
        {
            var player = (Player) Context.Items["player"];
            if (!player.lastAttachedGameId.HasValue)
            {
                _log.LogWarning("PlayerReady called with empty gameId");
                return;
            }

            var gameIdGuid = player.lastAttachedGameId.Value.ToString();
            var game = GamesState.Games.FirstOrDefault(g => g.GameId == player.lastAttachedGameId);
            if (game != null)
            {
                if (player != null)
                {
                    if (game.PhaseDetails.CurrentPhase == RoundPhase.Lobby)
                    {
                        player.IsReady = !player.IsReady; 
                    }
                    else
                    {
                        player.IsReady = true;
                    }
                    await _notifier.PlayerReady(game, player);
                    await _notifier.SendPlayersInRoom(game);
                    _log.LogInformation("Player {Username} in Game {GameId} is ready={ready}", player.Username, gameIdGuid, player.IsReady);
                    // Check if all players are ready
                    var nonRemovedPlayers = game.Players.Where(p => !p.Player.IsRemoved).ToArray();
                    if (nonRemovedPlayers.All(p => p.Player.IsReady) && 
                        (game.PhaseDetails.CurrentPhase != RoundPhase.Lobby || nonRemovedPlayers.Length >= 3))
                    {
                        if (game.PhaseDetails.CurrentPhase == RoundPhase.Lobby)
                        {
                            var toRemove = game.Players.Where(p => p.Player.IsRemoved).ToArray();
                            foreach (var pr in toRemove)
                            {
                                pr.Player.lastAttachedGameId = null;
                                _players.Remove(pr.Player.UserToken);
                                game.Players.Remove(pr);
                            }
                        }

                        _log.LogInformation("All players in Game {GameId} are ready. Advancing phase.", gameIdGuid);
                        await _orchestrator.EndPhase(game, "All players ready");
                    }
                }
            }
            else
            {
                _log.LogWarning("PlayerReady called but game not found: {gameId}", gameIdGuid);
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
                var innerPlayer = (Player) Context.Items["player"];
                var player = game.Players.FirstOrDefault(p => p.Player == innerPlayer);
                if (player != null)
                {
                    player.VotedPlayerId = selectedPlayerId;
                    _log.LogInformation("Player {Username} in Game {GameId} casted vote", player.Player.Username, gameIdGuid);
                    // Check if all players have submitted their drawings
                }
            }
        }

        private async Task detachPlayerFromGame(Player player, Game game)
        {
            player.IsRemoved = true;
            if (game.Players.All(p => p.Player.IsRemoved))
            {
                foreach (var newPlayer in game.Players)
                {
                    _players.Remove(newPlayer.Player.UserToken);
                }
                game.Players.Clear();
                GamesState.Games.Remove(game);
                Context.Items.Remove("player");
                return;
            }

            await _notifier.SendPlayersInRoom(game);
        }

    }
}