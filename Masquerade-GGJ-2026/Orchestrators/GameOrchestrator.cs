using Masquerade_GGJ_2026.Models;

namespace Masquerade_GGJ_2026.Orchestrators
{
    public class GameOrchestrator
    {
        private readonly Random _random = new Random();
        private readonly GameNotifier _notifier;

        public GameOrchestrator(GameNotifier notifier)
        {
            _notifier = notifier;
        }

        public void GenerateNewMaskRequirements(Game game)
        {
            var maskDescriptions = new List<string>
            {
                "A mysterious mask with intricate patterns.",
                "A playful mask adorned with feathers.",
                "A dark mask that conceals all emotions.",
                "A vibrant mask full of colors and joy.",
                "A mask of polished brass with tiny clockwork gears.",
                "A cracked porcelain mask with fading blush.",
                "A mask painted like a moonlit sea, waves in silver.",
                "A mask encrusted with tiny mirrors catching every light.",
                "A translucent mask that glows faintly at dusk.",
                "A mask woven from dried vines and autumn leaves.",
                "A minimalist matte-black mask with a single silver line.",
                "A mask with a fixed stern smile and hollow, observing eyes.",
                "A childlike mask with oversized painted eyes and dashes.",
                "A mask stitched from colorful patchwork fabrics and thread.",
                "A mask shaped like a fox, slim and cunning in expression.",
                "A mask with cascading ribbons that whisper when it moves.",
                "A mask adorned with tiny bells that chime softly on motion.",
                "A mask that seems to change expression when viewed from different angles.",
                "A mask with faintly glowing runes etched along its rim.",
                "A delicate lace mask studded with miniature pearls.",
                "A mask painted in bold asymmetric strokes of crimson and gold.",
                "A mask that appears to have water trapped behind its surface.",
                "A carved wooden mask with ancient symbols along the brow.",
                "A mask bearing smudges of charcoal and whimsical, hurried doodles."
            };

            for (int i = 0; i != game.TotalNumberOfRequirements; i++)
            {
                int index;
                string selectedDescription;
                do
                {
                    index = _random.Next(maskDescriptions.Count);
                    selectedDescription = maskDescriptions[index];
                } while (game.AllMaskRequirements.Contains(selectedDescription));
                game.AllMaskRequirements.Add(selectedDescription);
            }
        }

        public async Task NextPhase(Game game)
        {
            var phaseDetails = game.PhaseDetails;
            phaseDetails.PhaseCts?.Cancel();
            phaseDetails.PhaseCts = new CancellationTokenSource();

            phaseDetails.NextPhase();
            game.Players.ForEach(p => p.IsReady = false);

            switch (phaseDetails.CurrentPhase)
            {
                case RoundPhase.Lobby:
                    phaseDetails.PhaseEndsAt = null;
                    game.Players.ForEach(p => p.EncodedMask = null);
                    game.Players.ForEach(p => p.VotedPlayerId = null);
                    break;
                case RoundPhase.Drawing:
                    GenerateNewMaskRequirements(game);
                    phaseDetails.PhaseEndsAt = DateTime.UtcNow.AddSeconds(phaseDetails.DrawingTimeSeconds);
                    _ = EndPhaseAfterTimeout(game, phaseDetails.DrawingTimeSeconds, phaseDetails.PhaseCts.Token);
                    break;
                case RoundPhase.Voting:
                    phaseDetails.PhaseEndsAt = DateTime.UtcNow.AddSeconds(phaseDetails.VotingTimeSeconds);
                    _ = EndPhaseAfterTimeout(game, phaseDetails.VotingTimeSeconds, phaseDetails.PhaseCts.Token);
                    break;
                case RoundPhase.Scoreboard:
                    GrantPoints(game);
                    phaseDetails.PhaseEndsAt = null;
                    break;
                default:
                    throw new NotImplementedException();
            }

            await _notifier.PhaseChanged(game);
        }

        private void GrantPoints(Game game)
        {
            var badPlayer = game.Players.First(p => p.IsEvil);
            var kickedPlayerId = game.Players.GroupBy(p => p.VotedPlayerId).OrderByDescending(g => g.Count()).First().Key;
            foreach (var player in game.Players)
            {
                if (player != badPlayer)
                {
                    if (player.VotedPlayerId == badPlayer.ConnectionId)
                    {
                        player.Score += 5;
                    }
                    if (badPlayer.ConnectionId == kickedPlayerId)
                    {
                        player.Score += 5;
                    }
                }
                if (player == badPlayer && kickedPlayerId != badPlayer.ConnectionId)
                {
                    badPlayer.Score += 20;
                }
            }
        }

        async Task EndPhaseAfterTimeout(Game game, int delayInSeconds, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delayInSeconds), token);
                EndPhase(game, "Timeout");
            }
            catch (TaskCanceledException) { }
        }

        public async Task EndPhase(Game game, string reason)
        {
            await _notifier.EndPhase(game, reason);
            await NextPhase(game);
        }
    }
}
