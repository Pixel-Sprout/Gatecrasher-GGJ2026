namespace Masquerade_GGJ_2026.Models.Messages
{
    using System.Collections.Generic;

    public class UserMask
    {
        public string UserName { get; set; } = string.Empty;
        public string EncodedMask { get; set; } = string.Empty;
    }

    public class VotingMessage
    {
        public List<UserMask> Masks { get; set; } = new();
        public DateTime PhaseEndsAt { get; set; }
    }
}