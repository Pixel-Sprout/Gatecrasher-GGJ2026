using Masquerade_GGJ_2026.Hubs;
using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Enums;
using Masquerade_GGJ_2026.Models.Messages;
using Microsoft.AspNetCore.SignalR;

namespace Masquerade_GGJ_2026.Notifiers
{
    public class PlayerNotifier : BaseNotifier
    {
        private readonly IHubContext<GameHub> _hub;
        private readonly ILogger<PlayerNotifier> _log;

        public PlayerNotifier(ILogger<PlayerNotifier> log, IHubContext<GameHub> hub)
        {
            _log = log;
            _hub = hub;
        }

        public async Task PhaseChanged(Player player, Game game)
        {
            _log.LogInformation("Sending PhaseChanged to Player {UserId}", player.UserId);
            bool isEvil = game.GetPlayerState(player).IsEvil;

            object? message = game.PhaseDetails.CurrentPhase switch
            {
                RoundPhase.Lobby => CreateLobbyMessage(game),
                RoundPhase.Drawing => CreateDrawingMessage(game, isEvil),
                RoundPhase.Voting => CreateVotingMessage(game),
                RoundPhase.Scoreboard => CreateScoreboardMessage(game),
                RoundPhase.CutsceneTheChoice => CreateCutsceneMessage(game),
                _ => null,
            };

            await _hub.Clients.Client(player.ConnectionId).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, message);
        }

        public async Task PlayerState(Player player)
        {
            await _hub.Clients.Client(player.ConnectionId).SendAsync("PlayerState", player.Username, player.UserId);
        }

        public async Task AllGameRooms(Player player, GameRoomMessage[] gameRooms)
        {
            await _hub.Clients.Client(player.ConnectionId).SendAsync("ReceiveAllGameIds", gameRooms);
        }
    }
}