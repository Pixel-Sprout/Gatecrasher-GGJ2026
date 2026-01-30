namespace Masquerade_GGJ_2026.Models
{
    public enum RoundPhase
    {
        Lobby,
        Drawing,
        Voting,
        Scoreboard
    }

    public class Game
    {
        public Guid GameId { get; set; } = Guid.NewGuid();
        public List<Player> Players { get; set; }
        public RoundPhase CurrentPhase { get; set; }
        public int DrawingTimeSeconds { get; set; }
        public int VotingTimeSeconds { get; set; } 
        public List<string> AllMaskRequirements { get; set; }

        public Game()
        {
            CurrentPhase = RoundPhase.Lobby;
            Players = new List<Player>();
            DrawingTimeSeconds = 60;
            VotingTimeSeconds = 30;
            AllMaskRequirements = new List<string>();
        }
    }
}