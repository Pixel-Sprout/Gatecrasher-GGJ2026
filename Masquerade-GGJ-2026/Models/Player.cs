namespace Masquerade_GGJ_2026.Models
{
    public class Player
    {
        public required string ConnectionId { get; set; }
        public string? Username { get; set; }
        public bool IsEvil { get; set; }
        public string? EncodedMask { get; set; }
        public string? VotedPlayerId { get; set; }
        public int Score { get; set; }
        public bool IsReady { get; set; }
    }
}