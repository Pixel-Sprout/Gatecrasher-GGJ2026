namespace Masquerade.Models.Messages;

public class GameRoomMessage
{
    public string GameId { get; set; }
    public string GameName { get; set; }
    public string CurrentPhase { get; set; }
}