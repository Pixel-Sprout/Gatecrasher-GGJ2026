namespace Masquerade.Models.Messages
{
    using System.Collections.Generic;

    public class DrawingMessage
    {
        public bool IsPlayerEvil { get; set; }
        public List<string> MaskDescriptions { get; set; } = new();
        public DateTime PhaseEndsAt { get; set; }
    }
}