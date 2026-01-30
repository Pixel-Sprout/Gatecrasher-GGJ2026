namespace Masquerade_GGJ_2026.Models.Messages
{
    using System.Collections.Generic;

    public class LobbyMessage
    {
        public List<Player> Players { get; set; } = new();
    }
}