namespace Masquerade_GGJ_2026.Models.Messages
{
    public class GameSettings
    {
        //public int MaxPlayers { get; set; } = 8;
        public int Rounds { get; set; } = 5;
        public int DrawingTimeSeconds { get; set; } = 120;
        public int VotingTimeSeconds { get; set; } = 30;
        public int TotalNumberOfRequirements { get; set; } = 6;
        public int GoodPlayerNumberOfRequirements { get; set; } = 4;
        public int BadPlayerNumberOfRequirements { get; set; } = 2;
        public bool UseLongMaskDescriptions { get; set; } = false;
    }
}