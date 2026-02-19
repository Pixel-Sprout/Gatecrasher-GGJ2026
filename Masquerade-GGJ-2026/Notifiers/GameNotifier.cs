using Masquerade_GGJ_2026.Hubs;
using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Masquerade_GGJ_2026.Notifiers
{
    public class GameNotifier : BaseNotifier
    {
        private readonly IHubContext<GameHub> _hub;
        private readonly ILogger<GameNotifier> _log;

        public GameNotifier(ILogger<GameNotifier> log, IHubContext<GameHub> hub)
        {
            _log = log;
            _hub = hub;
        }

        public async Task UserJoined(string gameId, Player player)
        {
            player.lastAttachedGameId = gameId;
            await _hub.Clients.Group(gameId).SendAsync("UserJoinedGameGroup", player.UserId, player.Username, gameId);
            await _hub.Groups.AddToGroupAsync(player.ConnectionId, gameId);
            _log.LogInformation("Connection {ConnectionId} joined game group {GameId}", player.UserId, gameId);
        }

        public async Task UserLeft(string gameId, Player player)
        {
            await _hub.Clients.Group(gameId).SendAsync("UserLeftGameGroup", player.UserId, player.Username, gameId);
            await _hub.Groups.RemoveFromGroupAsync(player.ConnectionId, gameId);
            _log.LogInformation("Connection {ConnectionId} left game group {GameId}", player.UserId, gameId);
        }

        public async Task PhaseChanged(Game game)
        {
            _log.LogInformation("Broadcasting PhaseChanged for GameId {GameId} to group", game.GameId);

            switch (game.PhaseDetails.CurrentPhase)
            {
                case RoundPhase.Lobby:
                    await _hub.Clients.Group(game.GameId).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateLobbyMessage(game));
                    break;
                case RoundPhase.Drawing:
                    var badPlayer = game.Players[_random.Next(game.Players.Count)];
                    foreach (var player in game.Players)
                    {
                        player.IsEvil = player == badPlayer;
                        await player.Player.NotifyPhaseChanged(game);
                    }
                    break;
                case RoundPhase.Voting:
                    await _hub.Clients.Group(game.GameId).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateVotingMessage(game));
                    break;
                case RoundPhase.Scoreboard:
                    await _hub.Clients.Group(game.GameId).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateScoreboardMessage(game));
                    break;
                case RoundPhase.CutsceneTheChoice:
                    await _hub.Clients.Group(game.GameId).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateCutsceneMessage(game));
                    break;
                default:
                    await _hub.Clients.Group(game.GameId).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, null);
                    break;
            }
        }

        public async Task EndPhase(Game game, string reason)
        {
            await _hub.Clients.Group(game.GameId).SendAsync("PhaseEnded", game.PhaseDetails.CurrentPhase, reason);
        }

        public async Task PlayerReady(Game game, Player player)
        {
            await _hub.Clients.Group(game.GameId).SendAsync("PlayerIsReady", player.UserId, player.IsReady);
        }

        public async Task SendPlayersInRoom(Game game)
        {
            await _hub.Clients.Group(game.GameId).SendAsync("PlayersInTheRoom",
                game.Players.Where(p => !p.IsRemoved).Select(p => p.Player).ToList());
        }

        public async Task GameSettingsUpdated(Game game)
        {
            await _hub.Clients.Group(game.GameId).SendAsync("GameSettingsUpdated", game.Settings);
        }
    }
}
