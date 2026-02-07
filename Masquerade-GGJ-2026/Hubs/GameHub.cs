using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Messages;
using Masquerade_GGJ_2026.Orchestrators;
using Masquerade_GGJ_2026.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace Masquerade_GGJ_2026.Hubs
{
    public class GameHub : Hub
    {
        private readonly ILogger<GameHub> _log;
        private readonly PlayerFactory _playerFactory;
        private readonly GameOrchestrator _orchestrator;
        private readonly IGameStore _gameStore;
        private static IDictionary<string, Player> _players = new Dictionary<string, Player>();

        public GameHub(ILogger<GameHub> log,
            IGameStore gameStore,
            PlayerFactory playerFactory,
            GameOrchestrator orchestrator)
        {
            _log = log;
            _gameStore = gameStore;
            _playerFactory = playerFactory;
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
                if (_players.TryGetValue(userToken, out Player? player))
                {
                    player.ConnectionId = Context.ConnectionId;
                    player.IsRemoved = false;

                    if (!string.IsNullOrEmpty(player.lastAttachedGameId))
                    {
                        Context.Items["gameId"] = player.lastAttachedGameId;
                        Context.Items["username"] = player.Username;
                        Context.Items["player"] = player;
                        var game = _gameStore.Get(player.lastAttachedGameId);
                        if (game != null)
                        {
                            var playerState = game.Players.FirstOrDefault(p => p.Player == player);
                            if (playerState == null)
                            {
                                game.Players.Add(new PlayerGameState { Player = player });
                            }

                            await game.NotifyUserJoined(player);
                            await player.NotifyPlayerState();
                            await player.NotifyPhaseChanged(game);
                            await game.NotifySendPlayersInRoom();
                        }
                    }
                }
                else
                {
                    player = _playerFactory.Create(userToken, Context.ConnectionId, username);
                    _players.Add(userToken, player);
                }

                player.IsRemoved = false;
                Context.Items["username"] = username;
                Context.Items["player"] = player;
                await player.NotifyPlayerState();
                _log.LogInformation("User connected to GameHub: {Username} [{UserToken}], ConnectionId: {ConnectionId}", username, userToken, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            _log.LogInformation("User disconnected from GameHub: {Username}, ConnectionId: {ConnectionId}", username, Context.ConnectionId);

            // If the connection was in a game group, notify that group and remove the connection
            if (Context.Items.ContainsKey("gameId"))
            {
                var gameId = Context.Items["gameId"]?.ToString();
                var game = _gameStore.Get(gameId);
                var player = (Player)Context.Items["player"]!;
                if (game != null && player != null)
                {
                    await DetachPlayerFromGame(player, game);
                    await game.NotifyUserLeft(player);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Broadcast all game ids to the connected clients
        public async Task GetAllGameIds()
        {
            var player = (Player)Context.Items["player"]!;
            GameRoomMessage[] gameRooms = _gameStore.GetAllGames()
                    .Select(g => new GameRoomMessage()
                    {
                        GameId = g.GameId,
                        GameName = g.GameName,
                        CurrentPhase = g.PhaseDetails.CurrentPhase.ToString()
                    }).ToArray();
            await player.NotifyAllGameRooms(gameRooms);
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

            var game = _gameStore.Get(gameId);
            if (game == null || game.PhaseDetails.CurrentPhase != RoundPhase.Lobby)
            {
                await Clients.Caller.SendAsync("Error", "Game not found or already started.");
                return;
            }

            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            Context.Items["gameId"] = gameId;

            var player = (Player)Context.Items["player"]!;

            await game.NotifyUserJoined(player);
            await player.NotifyPhaseChanged(game);
            await game.NotifySendPlayersInRoom();
        }

        public async Task<string> CreateAndJoinGame(string gameName)
        {
            //Create new game via factory (it assigns per-game notifier)
            var newGame = _gameStore.Create(string.IsNullOrWhiteSpace(gameName) ? null : gameName);

            await _orchestrator.EndPhase(newGame, "New Game");
            //Join the newly created game
            await JoinGame(newGame.GameId);

            await newGame.NotifyPhaseChanged();

            return newGame.GameId;
        }

        /// <summary>
        /// Allows a connected client to leave a specific game group.
        /// </summary>
        public async Task LeaveGame()
        {
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            Context.Items.Remove("gameId");
            var player = (Player)Context.Items["player"]!;

            var game = _gameStore.Get(gameId);
            if (game != null)
            {
                await game.NotifyUserLeft(player);
                await DetachPlayerFromGame(player, game);
            }
        }

        public async Task PlayerReady()
        {
            var player = (Player)Context.Items["player"]!;
            if (string.IsNullOrWhiteSpace(player.lastAttachedGameId))
            {
                _log.LogWarning("PlayerReady called with empty gameId");
                return;
            }

            var gameIdGuid = player.lastAttachedGameId;
            var game = _gameStore.Get(player.lastAttachedGameId);
            if (game != null)
            {
                if (game.PhaseDetails.CurrentPhase == RoundPhase.Lobby)
                {
                    player.IsReady = !player.IsReady;
                }
                else
                {
                    player.IsReady = true;
                }
                await game.NotifyPlayerReady(player);
                await game.NotifySendPlayersInRoom();
                _log.LogInformation("Player {Username} in Game {GameId} is ready={ready}", player.Username, gameIdGuid, player.IsReady);
                // Check if all players are ready
                var nonRemovedPlayers = game.Players.Where(p => !p.Player.IsRemoved).ToArray();
                if (nonRemovedPlayers.All(p => p.Player.IsReady)
                    && (game.PhaseDetails.CurrentPhase != RoundPhase.Lobby || nonRemovedPlayers.Length >= 3))
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
            else
            {
                _log.LogWarning("PlayerReady called but game not found: {gameId}", gameIdGuid);
            }
        }

        public async Task CastVote(string selectedPlayerId)
        {
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            var game = _gameStore.Get(gameId);
            if (game != null)
            {
                var player = (Player)Context.Items["player"];
                var playerState = game.Players.FirstOrDefault(p => p.Player == player);
                if (playerState != null)
                {
                    playerState.VotedPlayerId = selectedPlayerId;
                    _log.LogInformation("Player {Username} in Game {GameId} casted vote", player.Username, gameId);
                }
            }
        }

        private async Task DetachPlayerFromGame(Player player, Game game)
        {
            player.IsRemoved = true;
            if (game.Players.All(p => p.Player.IsRemoved))
            {
                foreach (var newPlayer in game.Players)
                {
                    _players.Remove(newPlayer.Player.UserToken);
                }
                game.Players.Clear();
                _gameStore.Remove(game.GameId);
                Context.Items.Remove("player");
                return;
            }

            await game.NotifySendPlayersInRoom();
        }

        public async Task UpdateGameSettings(GameSettings settings)
        {
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            var game = _gameStore.Get(gameId);
            if (game != null)
            {
                var player = (Player)Context.Items["player"]!;
                if (game.Players.First(x => !x.Player.IsRemoved).Player != player)
                {
                    return;
                }
                game.Settings.DrawingTimeSeconds = settings.DrawingTimeSeconds;
                game.Settings.VotingTimeSeconds = settings.VotingTimeSeconds;
                game.Settings.Rounds = settings.Rounds;
                await game.NotifyGameSettingsUpdated();
            }
        }
    }
}