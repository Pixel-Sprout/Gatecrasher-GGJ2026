using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Models.Messages;

namespace Masquerade_GGJ_2026.Orchestrators
{
    public static class GameOrchestrator
    {
        public static Random random = new Random();

        public static ScoreboardMessage CreateScoreboardMessage(Game game)
        {
            return new ScoreboardMessage
            {
                Players = game.Players
            };
        }

        public static VotingMessage CreateVotingMessage(Game game)
        {
            return new VotingMessage
            {
                Masks = game.Players.Select(p => (p.Username, p.EncodedMask)).ToList()
            };
        }

        public static DrawingMessage CreateDrawingMessage(Game game, bool isPlayerEvil)
        {
            var message = new DrawingMessage
            {
                IsPlayerEvil = isPlayerEvil,
            };

            for (int i = 0; i != (isPlayerEvil ? 2 : 4); i++)
            {
                int index;
                string selectedDescription;
                do
                {
                    index = random.Next(game.AllMaskRequirements.Count);
                    selectedDescription = game.AllMaskRequirements[index];
                } while (message.MaskDescriptions.Contains(selectedDescription));
                message.MaskDescriptions.Add(selectedDescription);
            }
            return message;
        }

        public static LobbyMessage CreateLobbyMessage(Game game)
        {
            return new LobbyMessage
            {
                Players = game.Players
            };
        }

        public static void GenerateNewMaskRequirements(Game game)
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

            for (int i = 0; i != 6; i++)
            {
                int index;
                string selectedDescription;
                do
                {
                    index = random.Next(maskDescriptions.Count);
                    selectedDescription = maskDescriptions[index];
                } while (game.AllMaskRequirements.Contains(selectedDescription));
                game.AllMaskRequirements.Add(selectedDescription);
            }
        }
    }
}
