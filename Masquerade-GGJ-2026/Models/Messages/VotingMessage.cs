namespace Masquerade_GGJ_2026.Models.Messages
{
    using System.Collections.Generic;

    public class VotingMessage
    {
        public List<(string userName, string encodedMask)> Masks { get; set; } = new();
    }
}