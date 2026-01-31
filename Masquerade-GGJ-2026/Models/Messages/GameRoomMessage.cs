namespace Masquerade_GGJ_2026.Models.Messages;

public class GameRoomMessage
{
    public Guid GameId { get; set; }
    public string GameName { get; set; }
    public string CurrentPhase { get; set; }
}