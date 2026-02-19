using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Enums;

namespace Masquerade_GGJ_2026.Orchestrators
{
    public class GameOrchestrator
    {
        private readonly Random _random = new Random();
        private readonly ILogger<GameOrchestrator> _log;

        public GameOrchestrator(ILogger<GameOrchestrator> log)
        {
            _log = log;
        }

        public void GenerateNewMaskRequirements(Game game)
        {
            var maskDescriptions = new List<string>
            {
                "A mysterious mask with repeating patterns.",
                "A dark mask that hides all emotion.",
                "A bright mask full of color and joy.",
                "A mask that feels mechanical or artificial.",
                "A slightly cracked mask.",
                "A mask that feels calm and quiet, like night.",
                "A mask that appears faintly glowing.",
                "A mask inspired by nature and outdoor shapes.",
                "A minimalist mask with very few details.",
                "A mask with a serious expression and empty eyes.",
                "A childlike mask with exaggerated features.",
                "A mask made of mixed colors that do not fully match.",
                "A mask that feels clever or sneaky.",
                "A mask that suggests movement or flow.",
                "A mask that seems to change expression when viewed differently.",
                "A mask with strange markings or symbols.",
                "A mask that feels light, soft, and decorative.",
                "A mask painted with bold, uneven strokes.",
                "A mask that feels old and worn by time.",
                "A mask with messy marks and playful drawings.",
                "A mask that feels calm and balanced.",
                "A mask that looks friendly at first, but strange up close.",
                "A mask with uneven shapes that still feel intentional.",
                "A mask that feels cold and distant.",
                "A mask that feels warm and welcoming.",
                "A mask that looks unfinished or incomplete.",
                "A mask with strong contrast between light and dark.",
                "A mask that feels emotional without showing a clear face.",
                "A mask that looks simple but expressive.",
                "A mask that feels festive but slightly tired.",
                "A mask that feels personal, like it belongs to someone.",
                "A mask with shapes that repeat imperfectly.",
                "A mask that feels serious and ceremonial.",
                "A mask that feels playful but slightly chaotic.",
                "A mask that feels quiet and watchful.",
            }; //TODO: Move to configuration file or database

            game.AllMaskRequirements.Clear();
            for (int i = 0; i != game.Settings.TotalNumberOfRequirements; i++)
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
            _log.LogInformation($"Generated new mask requirements for game {game.GameId}: {string.Join(", ", game.AllMaskRequirements)}");
        }

        public async Task NextPhase(Game game)
        {
            var phaseDetails = game.PhaseDetails;
            phaseDetails.PhaseCts?.Cancel();
            phaseDetails.PhaseCts = new CancellationTokenSource();

            phaseDetails.NextPhase();
            game.Players.ForEach(p => p.Player.IsReady = false);

            switch (phaseDetails.CurrentPhase)
            {
                case RoundPhase.Lobby:
                    phaseDetails.PhaseEndsAt = null;
                    game.Players.ForEach(p => p.EncodedMask = null);
                    game.Players.ForEach(p => p.VotedPlayerId = null);
                    break;
                case RoundPhase.Drawing:
                    GenerateNewMaskRequirements(game);
                    phaseDetails.PhaseEndsAt = DateTime.UtcNow.AddSeconds(game.Settings.DrawingTimeSeconds);
                    _ = EndPhaseAfterTimeout(game, game.Settings.DrawingTimeSeconds, phaseDetails.PhaseCts.Token);
                    break;
                case RoundPhase.Voting:
                    phaseDetails.PhaseEndsAt = DateTime.UtcNow.AddSeconds(game.Settings.VotingTimeSeconds);
                    _ = EndPhaseAfterTimeout(game, game.Settings.VotingTimeSeconds, phaseDetails.PhaseCts.Token);
                    break;
                case RoundPhase.Scoreboard:
                    GrantPoints(game);
                    phaseDetails.PhaseEndsAt = null;
                    break;
                default:
                    phaseDetails.PhaseEndsAt = null;
                    break;
            }

            await game.NotifyPhaseChanged();
            _log.LogInformation($"Game {game.GameId} moved to phase {phaseDetails.CurrentPhase}");
        }

        private void GrantPoints(Game game)
        {
            var badPlayer = game.Players.First(p => p.IsEvil);
            foreach (var player in game.Players)
            {
                switch (game.GetGameResult())
                {
                    case GameResult.BadWin:
                        if (player.IsEvil)
                        {
                            player.Score += 20;
                            _log.LogInformation($"Player {player.Player.UserId} (Evil) wins and receives 20 points.");
                        }
                        break;
                    case GameResult.GoodWin:
                        if (!player.IsEvil)
                        {
                            player.Score += 5;
                            _log.LogInformation($"Player {player.Player.UserId} (Good) wins and receives 5 points.");
                        }
                        break;
                    case GameResult.Tie:
                        if (player.IsEvil)
                        {
                            player.Score += 10;
                            _log.LogInformation($"Player {player.Player.UserId} (Evil) ties and receives 10 points.");
                        }
                        break;
                }
                if (player != badPlayer && player.VotedPlayerId == badPlayer.Player.UserId)
                {
                    player.Score += 5;
                    _log.LogInformation($"Player {player.Player.UserId} (Good) voted right and receives 5 points.");
                }
            }
        }

        async Task EndPhaseAfterTimeout(Game game, int delayInSeconds, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delayInSeconds), token);
                _log.LogInformation($"Phase timeout reached for game {game.GameId} in phase {game.PhaseDetails.CurrentPhase}");
                EndPhase(game, "Timeout");
            }
            catch (TaskCanceledException) { }
        }

        public async Task EndPhase(Game game, string reason)
        {
            await game.NotifyEndPhase(reason);
            await NextPhase(game);
        }

        public void ClearRemovedUsers(Game game)
        {
            var removedPlayers = game.Players.Where(p => p.IsRemoved).ToList();
            foreach (var removedPlayer in removedPlayers)
            {
                game.Players.Remove(removedPlayer);
                _log.LogInformation($"Removed player {removedPlayer.Player.UserId} from game {game.GameId}");
            }
        }
    }
}
