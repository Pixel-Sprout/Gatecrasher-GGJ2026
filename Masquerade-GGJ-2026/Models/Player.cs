namespace Masquerade_GGJ_2026.Models
{
    public class Player
    {
        public required string UserToken { get; set; }
        public readonly string UserId = Guid.NewGuid().ToString();
        public required string ConnectionId { get; set; }
        public string? Username { get; set; }
        public bool IsReady { get; set; }
        public bool IsRemoved { get; set; } = false;
        public Guid? lastAttachedGameId { get; set; } = null;
    }
}