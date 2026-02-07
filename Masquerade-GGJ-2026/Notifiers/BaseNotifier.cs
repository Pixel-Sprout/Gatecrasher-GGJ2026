using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Messages;

namespace Masquerade_GGJ_2026.Notifiers
{
    public abstract class BaseNotifier
    {
        protected readonly Random _random = new Random();

        protected ScoreboardMessage CreateScoreboardMessage(Game game)
        {
            return new ScoreboardMessage
            {
                Players = game.Players
            };
        }

        protected CutsceneMessage CreateCutsceneMessage(Game game)
        {
            bool shouldPlayAlternativeScene = false;

            switch (game.PhaseDetails.CurrentPhase)
            {
                case RoundPhase.CutsceneTheChoice:
                    shouldPlayAlternativeScene = game.IsBadEnding();
                    break;
            }

            return new CutsceneMessage
            {
                PlayAlternativeCutscene = shouldPlayAlternativeScene
            };
        }

        protected VotingMessage CreateVotingMessage(Game game)
        {
            return new VotingMessage
            {
                Masks = game.Players.Select(p => new UserMask { Player = p.Player, EncodedMask = p.EncodedMask ?? "" }).ToList(),
                PhaseEndsAt = game.PhaseDetails.PhaseEndsAt!.Value,
            };
        }

        protected DrawingMessage CreateDrawingMessage(Game game, bool isPlayerEvil)
        {
            var message = new DrawingMessage
            {
                IsPlayerEvil = isPlayerEvil,
                PhaseEndsAt = game.PhaseDetails.PhaseEndsAt!.Value,
            };

            for (int i = 0; i != (isPlayerEvil ? game.Settings.BadPlayerNumberOfRequirements : game.Settings.GoodPlayerNumberOfRequirements); i++)
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

        protected LobbyMessage CreateLobbyMessage(Game game)
        {
            return new LobbyMessage
            {
                Players = game.Players.Where(p => !p.Player.IsRemoved).Select(p => p.Player).ToList(),
                Settings = game.Settings
            };
        }

        public async Task SendExceptionMessage(Game game, string message, string stackTrace)
        {
            //await _hub.Clients.Group(game.GameId).SendAsync("ExceptionMessage", message, stackTrace);
        }
    }
}