namespace Masquerade_GGJ_2026.Models
{
    public class Game
    {
        public Guid GameId { get; set; } = Guid.NewGuid();
        public string GameName { get; set; }
        public List<PlayerGameState> Players { get; set; }
        public PhaseDetails PhaseDetails { get; set; }
        public List<string> AllMaskRequirements { get; set; }
        public int TotalNumberOfRequirements { get; set; } = 6;
        public int GoodPlayerNumberOfRequirements { get; set; } = 4;
        public int BadPlayerNumberOfRequirements { get; set; } = 2;

        public Game()
        {
            GameName = $"Room {GameId.ToString()}";
            PhaseDetails = new PhaseDetails(drawingTimeSeconds: 60, votingTimeSeconds: 30);
            Players = new List<PlayerGameState>();
            AllMaskRequirements = new List<string>();
        }
    }
}