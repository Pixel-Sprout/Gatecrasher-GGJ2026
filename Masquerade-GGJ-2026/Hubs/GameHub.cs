using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Enums;
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
                    player.IsConnected = true;
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
                            else
                            {
                                playerState.IsRemoved = false;
                            }

                            await game.NotifyUserJoined(player);
                            await player.NotifyPlayerState();
                            await player.NotifyPhaseChanged(game);
                            await game.NotifySendPlayersInRoom();
                            _log.LogInformation("Player {UserId} reconnected to a game {GameId}", player.UserId, game.GameId);
                        }
                    }
                }
                else
                {
                    player = _playerFactory.Create(userToken, Context.ConnectionId, username);
                    _players.Add(userToken, player);
                }
                Context.Items["username"] = username;
                Context.Items["player"] = player;
                await player.NotifyPlayerState();
                _log.LogInformation("User {UserId} ({Username}) connected", player.UserId, player.Username);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            var player = (Player)Context.Items["player"]!;
            player.IsConnected = false;
            _log.LogInformation("User {UserId} disconnected", player.UserId);

            // If the connection was in a game group, notify that group and remove the connection
            if (Context.Items.ContainsKey("gameId"))
            {
                var gameId = Context.Items["gameId"]?.ToString();
                var game = _gameStore.Get(gameId);
                if (game != null)
                {
                    await DetachPlayerFromGame(player, game);
                    await game.NotifyUserLeft(player);
                    Context.Items.Remove("player");
                    _log.LogInformation("Player {UserId} left Game {GameId} due to disconnection", player.UserId, gameId);
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
            _log.LogInformation("User {UserId} requested game list. Returning {GameCount} games.", player.UserId, gameRooms.Length);
            await player.NotifyAllGameRooms(gameRooms);
        }

        /// <summary>
        /// Allows a connected client to join a specific game by id.
        /// Adds the connection to the group, stores the gameId in Context.Items,
        /// and broadcasts the join to members of that game only.
        /// </summary>
        public async Task JoinGame(string gameId)
        {
            var player = (Player)Context.Items["player"]!;
            //Already in a different game
            if (Context.Items["gameId"] != null)
            {
                _log.LogWarning("Player {UserId} attempted to join Game {GameId} but is already in Game {CurrentGameId}", player.UserId, gameId, Context.Items["gameId"]);
                await Clients.Caller.SendAsync("Error", "Already in a game. Leave current game before joining another.");
                return;
            }

            var game = _gameStore.Get(gameId);
            if (game == null || game.PhaseDetails.CurrentPhase != RoundPhase.Lobby)
            {
                _log.LogWarning("Player {UserId} attempted to join Game {GameId} but it was not found or already started", player.UserId, gameId);
                await Clients.Caller.SendAsync("Error", "Game not found or already started.");
                return;
            }

            var username = Context.Items.ContainsKey("username") ? Context.Items["username"]?.ToString() : null;
            Context.Items["gameId"] = gameId;


            await game.NotifyUserJoined(player);
            await player.NotifyPhaseChanged(game);
            await game.NotifySendPlayersInRoom();
            _log.LogInformation("Player {UserId} joined Game {GameId}", player.UserId, gameId);
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
            var player = (Player)Context.Items["player"]!;
            player.lastAttachedGameId = null;
            _log.LogInformation("Player {UserId} requested to leave their current game", player.UserId);
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            Context.Items.Remove("gameId");

            var game = _gameStore.Get(gameId);
            if (game != null)
            {
                await game.NotifyUserLeft(player);
                game.Players.RemoveAll(p=>p.Player == player);
                await DetachPlayerFromGame(player, game);
                await player.NotifyGetBackToUserSelect(game, "Left the game");
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
                _log.LogInformation("Player {UserId} in Game {GameId} is ready={ready}", player.UserId, game.GameId, player.IsReady);
                // Check if all players are ready
                var nonRemovedPlayers = game.Players.Where(p => !p.IsRemoved).ToArray();
                if (nonRemovedPlayers.All(p => p.Player.IsReady)
                    && (game.PhaseDetails.CurrentPhase != RoundPhase.Lobby || nonRemovedPlayers.Length >= 3))
                {
                    if (game.PhaseDetails.CurrentPhase == RoundPhase.Lobby)
                    {
                        var toRemove = game.Players.Where(p => p.IsRemoved).ToArray();
                        foreach (var pr in toRemove)
                        {
                            pr.Player.lastAttachedGameId = null;
                            if (!pr.Player.IsConnected)
                            {
                                _players.Remove(pr.Player.UserToken);
                                _log.LogInformation("Player {UserId} removed from player store because they were not connected on getting back to game lobby", pr.Player.UserId);
                            }
                        }
                        _orchestrator.ClearRemovedUsers(game);
                    }

                    _log.LogInformation("All players in Game {GameId} are ready. Advancing phase.", game.GameId);
                    await _orchestrator.EndPhase(game, "All players ready");
                }
            }
            else
            {
                _log.LogWarning("PlayerReady called but game not found: {gameId}", player.lastAttachedGameId);
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
                    _log.LogInformation("Player {UserId} in Game {GameId} casted vote for {SelectedPlayerId}", player.UserId, gameId, selectedPlayerId);
                }
            }
        }

        private async Task DetachPlayerFromGame(Player player, Game game)
        {
            var playerState = game.Players.FirstOrDefault(p => p.Player == player);
            if(playerState != null)
            {
                playerState.IsRemoved = true;
            }
            if (game.Players.All(p => p.IsRemoved))
            {
                foreach (var newPlayer in game.Players)
                {
                    if(!newPlayer.Player.IsConnected)
                    {
                        _players.Remove(newPlayer.Player.UserToken);
                        _log.LogInformation("Player {UserId} removed from player store because the game they were in was closed", newPlayer.Player.UserId);
                    }
                }
                game.Players.Clear();
                _gameStore.Remove(game.GameId);
                _log.LogInformation("Game {GameId} removed from store because all players left", game.GameId);
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
                if (game.Players.First(x => !x.IsRemoved).Player != player)
                {
                    _log.LogWarning("Player {UserId} attempted to update settings for Game {GameId} but is not the host", player.UserId, gameId);
                    return;
                }
                game.Settings.DrawingTimeSeconds = settings.DrawingTimeSeconds;
                game.Settings.VotingTimeSeconds = settings.VotingTimeSeconds;
                game.Settings.Rounds = settings.Rounds;
                game.Settings.TotalNumberOfRequirements = settings.TotalNumberOfRequirements;
                game.Settings.GoodPlayerNumberOfRequirements = settings.GoodPlayerNumberOfRequirements;
                game.Settings.BadPlayerNumberOfRequirements = settings.BadPlayerNumberOfRequirements;
                game.Settings.UseLongMaskDescriptions = settings.UseLongMaskDescriptions;
                _log.LogInformation("Player {UserId} updated settings for Game {GameId}", player.UserId, gameId);
                await game.NotifyGameSettingsUpdated();
            }
        }

        public async Task KickPlayer(string playerToKickId)
        {
            var gameId = Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
            var game = _gameStore.Get(gameId);
            if (game != null)
            {
                var player = (Player)Context.Items["player"]!;
                if (game.Players.First(x => !x.IsRemoved).Player != player)
                {
                    _log.LogWarning("Player {UserId} attempted to kick another player {KickedUserId} from Game {GameId} but is not the host", player.UserId, playerToKickId, gameId);
                    return;
                }
                var kickedPlayer = game.Players.First(x=>x.Player.UserId == playerToKickId).Player;
                await DetachPlayerFromGame(kickedPlayer, game);
                kickedPlayer.lastAttachedGameId = null;
                await kickedPlayer.NotifyGetBackToUserSelect(game, "Kicked by admin");
                _orchestrator.ClearRemovedUsers(game);
                _log.LogInformation("Player {UserId} was kicked from the Game {GameId}", kickedPlayer.UserId, gameId);
            }
        }
    }
}