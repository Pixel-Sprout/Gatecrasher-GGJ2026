namespace Masquerade.Models.Messages
{
    using System.Collections.Generic;

    public class ScoreboardMessage
    {
        public List<PlayerGameState> Players { get; set; } = new();
    }
}