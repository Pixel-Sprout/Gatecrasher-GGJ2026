using Masquerade_GGJ_2026.Models.Messages;

namespace Masquerade_GGJ_2026.Models
{
    public class Game
    {
        public Guid GameId { get; set; } = Guid.NewGuid();
        public string GameName { get; set; }
        public List<PlayerGameState> Players { get; set; }
        public PhaseDetails PhaseDetails { get; set; }
        public List<string> AllMaskRequirements { get; set; }
        public GameSettings Settings { get; set; } = new GameSettings();

        public Game()
        {
            GameName = $"Room {GameId.ToString()}";
            PhaseDetails = new PhaseDetails();
            Players = new List<PlayerGameState>();
            AllMaskRequirements = new List<string>();
        }
    }
}