namespace Masquerade.Models
{
    public class PlayerGameState
    {
        public required Player Player { get; set; }
        public bool IsEvil { get; set; }
        public string? EncodedMask { get; set; }
        public string? VotedPlayerId { get; set; }
        public int Score { get; set; }
        public bool IsRemoved { get; set; }
    }
}
