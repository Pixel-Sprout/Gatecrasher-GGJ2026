using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Enums;
using Masquerade_GGJ_2026.Models.Messages;
using Masquerade_GGJ_2026.Factories;
using Microsoft.AspNetCore.SignalR;

namespace Masquerade_GGJ_2026.Hubs;
public class GameHub(ILogger<GameHub> _log,
    GameRoomOrchestrator _gameRoomOrchestrator,
    PlayerFactory _playerFactory,
    GameOrchestrator _orchestrator) : Hub
{
    private static IDictionary<string, Player> _players = new Dictionary<string, Player>();
    
    private string? UserGameId() => Context.Items.ContainsKey("gameId") ? Context.Items["gameId"]?.ToString() : null;
    private Player? UserPlayer() => Context.Items.ContainsKey("player") ? (Player)Context.Items["player"]! : null;
    
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var username = httpContext?.Request.Query["username"].ToString();
        var userToken = httpContext?.Request.Query["userToken"].ToString();

        if (string.IsNullOrEmpty(username))
        {
            return;
        }
            
        if (string.IsNullOrEmpty(userToken))
        {
            userToken = Guid.NewGuid().ToString();
        }

        if (_players.TryGetValue(userToken, out Player? player))
        {
            var wasReattached = await TryReattachPlayerToGame(player);
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
        _log.LogInformation("User {UserId} ({Username}) connected", player.UserId, player.Username);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var player = (Player)Context.Items["player"]!;
        _log.LogInformation("User {UserId} disconnected", player.UserId);

        // If the connection was in a game group, notify that group and remove the connection
        var gameId = UserGameId();
        if (!string.IsNullOrEmpty(gameId))
        {
            var game = _gameRoomOrchestrator.GetGame(gameId);
            if (game != null)
            {
                await DetachPlayerFromGame(player, game);
                await game.NotifyUserLeft(player);
                _log.LogInformation("Player {UserId} left Game {GameId} due to disconnection", player.UserId, gameId);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<bool> TryReattachPlayerToGame(Player player)
    {
        player.ConnectionId = Context.ConnectionId;
        player.IsRemoved = false;

        if (string.IsNullOrEmpty(player.lastAttachedGameId))
            return false;
            
        Context.Items["gameId"] = player.lastAttachedGameId;
        Context.Items["username"] = player.Username;
        Context.Items["player"] = player;
        var game = _gameRoomOrchestrator.GetGame(player.lastAttachedGameId);
        if (game == null)
        {
            return false;
        }
            
        var playerState = game.Players.FirstOrDefault(p => p.Player == player);
        if (playerState == null)
        {
            game.Players.Add(new PlayerGameState { Player = player });
        }

        await _gameRoomOrchestrator.PlayerJoinGame(game, player, true);
        _log.LogInformation("Player {UserId} reconnected to a game {GameId}", player.UserId, game.GameId);
        return true;
    }
        
    // Broadcast all game ids to the connected clients
    /*[Obsolete("use something else. whatever, but not this!")]
    public async Task GetAllGameIds()
    {
        var player = UserPlayer();
        GameRoomMessage[] gameRooms = _gameRoomOrchestrator.GetActiveRoomMessage();
        _log.LogWarning("User {UserId} requested game list. Returning {GameCount} games. this call is obsolete!!!", player.UserId, gameRooms.Length);
        await player.NotifyAllGameRooms(gameRooms);
    }*/

    /// <summary>
    /// Allows a connected client to join a specific game by id.
    /// Adds the connection to the group, stores the gameId in Context.Items,
    /// and broadcasts the join to members of that game only.
    /// </summary>
    public async Task JoinGame(string gameId, Game? game = null)
    {
        var player = UserPlayer()!;
        //Already in a different game
        if (Context.Items["gameId"] != null)
        {
            _log.LogWarning("Player {UserId} attempted to join Game {GameId} but is already in Game {CurrentGameId}",
                player.UserId, gameId, Context.Items["gameId"]);
            await Clients.Caller.SendAsync("Error", "Already in a game. Leave current game before joining another.");
            return;
        }

        if (game == null)
        {
            game = _gameRoomOrchestrator.GetGame(gameId);
        }
        if (game == null || game.PhaseDetails.CurrentPhase != RoundPhase.Lobby)
        {
            _log.LogWarning("Player {UserId} attempted to join Game {GameId} but it was not found or already started", player.UserId, gameId);
            await Clients.Caller.SendAsync("Error", "Game not found or already started.");
            return;
        }
        
        Context.Items["gameId"] = game.GameId;
        await _gameRoomOrchestrator.PlayerJoinGame(game, player);
        _log.LogInformation("Player {UserId} joined Game {GameId}", player.UserId, gameId);
    }

    public async Task<string> CreateAndJoinGame(string gameName)
    {
        var newGame = _gameRoomOrchestrator.Create(gameName);
        await _orchestrator.EndPhase(newGame, "New Game");
        await JoinGame(newGame.GameId, newGame);

        await newGame.NotifyPhaseChanged();

        return newGame.GameId;
    }

    /// <summary>
    /// Allows a connected client to leave a specific game group.
    /// </summary>
    public async Task LeaveGame()
    {
        var player = UserPlayer()!;
        _log.LogInformation("Player {UserId} requested to leave their current game", player.UserId);
        var gameId = UserGameId();
        Context.Items.Remove("gameId");

        var game = _gameRoomOrchestrator.GetGame(gameId);
        if (game != null)
        {
            await game.NotifyUserLeft(player);
            await DetachPlayerFromGame(player, game);
        }
    }

    public async Task PlayerReady()
    {
        var player = UserPlayer()!;
        if (string.IsNullOrWhiteSpace(player.lastAttachedGameId))
        {
            _log.LogWarning("PlayerReady called with empty gameId");
            return;
        }

        var game =_gameRoomOrchestrator.GetGame(player.lastAttachedGameId);
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
        var gameId = UserGameId();
        var game = _gameRoomOrchestrator.GetGame(gameId);
        if (game != null)
        {
            var player = UserPlayer()!;
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
        player.IsRemoved = true;
        if (game.Players.All(p => p.Player.IsRemoved))
        {
            foreach (var newPlayer in game.Players)
            {
                _players.Remove(newPlayer.Player.UserToken);
            }
            game.Players.Clear();
            _gameRoomOrchestrator.Remove(game.GameId);
            _log.LogInformation("Game {GameId} removed from store because all players left", game.GameId);
            Context.Items.Remove("player"); //TODO: Think of a way where we don't remove players from the system if they went back to rooms list
            return;
        }

        await game.NotifySendPlayersInRoom();
    }

    public async Task UpdateGameSettings(GameSettings settings)
    {
        var gameId = UserGameId();
        var game = _gameRoomOrchestrator.GetGame(gameId);
        if (game != null)
        {
            var player = UserPlayer()!;
            if (game.Players.First(x => !x.Player.IsRemoved).Player != player)
            {
                _log.LogWarning("Player {UserId} attempted to update settings for Game {GameId} but is not the host", player.UserId, gameId);
                return;
            }
            game.Settings.DrawingTimeSeconds = settings.DrawingTimeSeconds;
            game.Settings.VotingTimeSeconds = settings.VotingTimeSeconds;
            game.Settings.Rounds = settings.Rounds;
            _log.LogInformation("Player {UserId} updated settings for Game {GameId}", player.UserId, gameId);
            await game.NotifyGameSettingsUpdated();
        }
    }
}