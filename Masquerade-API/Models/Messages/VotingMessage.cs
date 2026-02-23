namespace Masquerade.Models.Messages
{
    using System.Collections.Generic;

    public class UserMask
    {
        public Player Player { get; set; }
        public string EncodedMask { get; set; } = string.Empty;
    }

    public class VotingMessage
    {
        public List<UserMask> Masks { get; set; } = new();
        public DateTime PhaseEndsAt { get; set; }
    }
}