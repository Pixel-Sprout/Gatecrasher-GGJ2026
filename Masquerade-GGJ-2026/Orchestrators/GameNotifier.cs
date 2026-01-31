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

        public async Task UserJoined(string gameId, string playerId, string? userName)
        {
            await _hub.Clients.Group(gameId!).SendAsync("UserJoinedGameGroup", playerId, userName, gameId);
            await _hub.Groups.AddToGroupAsync(playerId, gameId);
            _log.LogInformation("Connection {ConnectionId} joined game group {GameId}", playerId, gameId);
        }

        public async Task UserLeft(string gameId, string playerId, string? userName)
        {
            await _hub.Clients.Group(gameId!).SendAsync("UserLeftGameGroup", playerId, userName, gameId);
            await _hub.Groups.RemoveFromGroupAsync(playerId, gameId!);
            _log.LogInformation("Connection {ConnectionId} left game group {GameId}", playerId, gameId);
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
                            await _hub.Clients.Client(player.ConnectionId).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateDrawingMessage(game, player.IsEvil));
                        }
                        break;
                    case RoundPhase.Voting:
                        await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateVotingMessage(game));
                        break;
                    case RoundPhase.Scoreboard:
                        await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PhaseChanged", game.PhaseDetails.CurrentPhase, CreateScoreboardMessage(game));
                        break;
                    default:
                        throw new ArgumentException("Invalid game phase");
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
                Masks = game.Players.Select(p => new UserMask { UserName = p.Username ?? "", EncodedMask = p.EncodedMask ?? "" }).ToList(),
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

        private LobbyMessage CreateLobbyMessage(Game game)
        {
            return new LobbyMessage
            {
                Players = game.Players
            };
        }


        public async Task PlayerReady(Game game, Player player)
        {
            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("PlayerIsReady", player.ConnectionId, player.IsReady);
        }

        public async Task UpdateGameState(Game game)
        {
            await _hub.Clients.Group(game.GameId.ToString()).SendAsync("GameStateUpdated", new
            {
                PhaseDetails = game.PhaseDetails,
            });
        }
    }
}
