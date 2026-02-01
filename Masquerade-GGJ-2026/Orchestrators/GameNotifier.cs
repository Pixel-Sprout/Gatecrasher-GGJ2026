using Masquerade_GGJ_2026.Hubs;
using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Messages;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Masquerade_GGJ_2026.Orchestrators
{
    public class GameNotifier
    {
        private readonly Random _random = new Random();
        private readonly IHubContext<GameHub> _hub;
        private readonly ILogger<GameHub> _log;

        public GameNotifier(ILogger<GameHub> log, IHubContext<GameHub> hub)
        {
            _log = log;
            _hub = hub;
        }

        public async Task UserJoined(Guid gameId, Player player)
        {
            player.lastAttachedGameId = gameId;
            var gameKey = gameId.ToString();
            await _hub.Clients.Group(gameKey!).SendAsync("UserJoinedGameGroup", player.UserId, player.Username, gameId);
            await _hub.Groups.AddToGroupAsync(player.ConnectionId, gameKey);
            _log.LogInformation("Connection {ConnectionId} joined game group {GameId}", player.UserId, gameId);
        }

        public async Task UserLeft(string gameId, Player player)
        {
            await _hub.Clients.Group(gameId!).SendAsync("UserLeftGameGroup", player.UserId, player.Username, gameId);
            await _hub.Groups.RemoveFromGroupAsync(player.ConnectionId, gameId!);
            _log.LogInformation("Connection {ConnectionId} left game group {GameId}", player.UserId, gameId);
        }

        public async Task PhaseChanged(Game? game)
        {
            if (game != null)
            {
                _log.LogInformation("Broadcasting PhaseChanged for GameId {GameId} to group", game.GameId);

                switch (game.PhaseDetails.CurrentPhase)
                {
                    case RoundPhase.Lobby:
                        await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateLobbyMessage(game));
                        break;
                    case RoundPhase.Drawing:
                        var badPlayer = game.Players[_random.Next(game.Players.Count)];
                        foreach (var player in game.Players)
                        {
                            player.IsEvil = player == badPlayer;
                            await _hub.Clients.Client(player.Player.ConnectionId).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateDrawingMessage(game, player.IsEvil));
                        }
                        break;
                    case RoundPhase.Voting:
                        await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateVotingMessage(game));
                        break;
                    case RoundPhase.Scoreboard:
                        await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateScoreboardMessage(game));
                        break;
                    default:
                        await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, null);
                        break;
                }
            }
        }

        public async Task EndPhase(Game game, string reason)
        {
            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PhaseEnded", game.PhaseDetails.CurrentPhase, reason);
        }

        private ScoreboardMessage CreateScoreboardMessage(Game game)
        {
            return new ScoreboardMessage
            {
                Players = game.Players
            };
        }

        private VotingMessage CreateVotingMessage(Game game)
        {
            return new VotingMessage
            {
                Masks = game.Players.Select(p => new UserMask { Player = p.Player, EncodedMask = p.EncodedMask ?? "" }).ToList(),
                PhaseEndsAt = game.PhaseDetails.PhaseEndsAt!.Value,
            };
        }

        private DrawingMessage CreateDrawingMessage(Game game, bool isPlayerEvil)
        {
            var message = new DrawingMessage
            {
                IsPlayerEvil = isPlayerEvil,
                PhaseEndsAt = game.PhaseDetails.PhaseEndsAt!.Value,
            };

            for (int i = 0; i != (isPlayerEvil ? game.BadPlayerNumberOfRequirements : game.GoodPlayerNumberOfRequirements); i++)
            {
                int index;
                string selectedDescription;
                do
                {
                    index = _random.Next(game.AllMaskRequirements.Count);
                    selectedDescription = game.AllMaskRequirements[index];
                } while (message.MaskDescriptions.Contains(selectedDescription));
                message.MaskDescriptions.Add(selectedDescription);
            }
            return message;
        }

        public LobbyMessage CreateLobbyMessage(Game game)
        {
            return new LobbyMessage
            {
                Players = game.Players.Select(p => p.Player).ToList()
            };
        }


        public async Task PlayerReady(Game game, Player player)
        {
            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PlayerIsReady", player.UserId, player.IsReady);
        }

        public async Task SendPlayersInRoom(Game game)
        {
            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PlayersInTheRoom", 
                game.Players.Where(p => !p.Player.IsRemoved).Select(p => p.Player).ToList());
        }

        public async Task SendExceptionMessage(Game game, string message, string stackTrace)
        {
            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("ExceptionMessage", message, stackTrace);
        }
    }
}
