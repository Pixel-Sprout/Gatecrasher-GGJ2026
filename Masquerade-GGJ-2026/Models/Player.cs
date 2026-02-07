using Masquerade_GGJ_2026.Models.Messages;
using Masquerade_GGJ_2026.Notifiers;

namespace Masquerade_GGJ_2026.Models
{
    public class Player
    {
        public string UserToken { get; set; }
        public string UserId { get; init; } = Guid.NewGuid().ToString();
        public string ConnectionId { get; set; }
        public string? Username { get; set; }
        public bool IsReady { get; set; }
        public bool IsRemoved { get; set; } = false;
        public string? lastAttachedGameId { get; set; } = null;
        public PlayerNotifier Notifier { get; set; }

        public Player(string userToken, string connectionId, string userName, PlayerNotifier notifier)
        {
            UserToken = userToken;
            ConnectionId = connectionId;
            Username = userName;
            Notifier = notifier;
        }

        #region Notifications

        public Task NotifyPhaseChanged(Game game)
        {
            return Notifier.PhaseChanged(this, game);
        }

        public Task NotifyPlayerState()
        {
            return Notifier.PlayerState(this);
        }

        public Task NotifyAllGameRooms(GameRoomMessage[] gameRooms)
        {
            return Notifier.AllGameRooms(this, gameRooms);
        }

        #endregion
    }
}