namespace Masquerade.Models.Messages
{
    using System.Collections.Generic;

    public class LobbyMessage
    {
        public List<Player> Players { get; set; } = new();
        public required GameSettings Settings { get; set; }
    }
}