using Masquerade_GGJ_2026.Models.Messages;
using Masquerade_GGJ_2026.Notifiers;

namespace Masquerade_GGJ_2026.Models
{
    public class Game
    {
        public string GameId { get; set; } = Guid.NewGuid().ToString();
        public string GameName { get; set; }
        public List<PlayerGameState> Players { get; set; }
        public PhaseDetails PhaseDetails { get; set; }
        public List<string> AllMaskRequirements { get; set; }
        public GameSettings Settings { get; set; } = new GameSettings();
        public GameNotifier Notifier { get; set; }

        public Game(string? gameName, GameNotifier notifier)
        {
            GameName = gameName ?? $"Room {GameId}";
            PhaseDetails = new PhaseDetails();
            Players = new List<PlayerGameState>();
            AllMaskRequirements = new List<string>();
            Notifier = notifier;
        }

        public bool IsBadEnding()
        {
            var badPlayer = Players.First(p => p.IsEvil);
            var groupedVotes = Players.GroupBy(p => p.VotedPlayerId).ToList();
            var kickedPlayers = groupedVotes.Where(g => g.Count() == groupedVotes.Max(x => x.Count())).Select(g => g.Key);
            return kickedPlayers.Count() != 1 || kickedPlayers.Single() != badPlayer.Player.UserId;
        }

        public PlayerGameState GetPlayerState(Player player)
        {
            return Players.Single(p => p.Player.UserId == player.UserId);
        }

        #region Notifications

        public Task NotifyPhaseChanged()
        {
            return Notifier.PhaseChanged(this);
        }

        public Task NotifyEndPhase(string reason)
        {
            return Notifier.EndPhase(this, reason);
        }

        public Task NotifyPlayerReady(Player player)
        {
            return Notifier.PlayerReady(this, player);
        }

        public Task NotifySendPlayersInRoom()
        {
            return Notifier.SendPlayersInRoom(this);
        }

        public Task NotifyExceptionMessage(string message, string stackTrace)
        {
            return Notifier.SendExceptionMessage(this, message, stackTrace);
        }

        public Task NotifyGameSettingsUpdated()
        {
            return Notifier.GameSettingsUpdated(this);
        }

        public Task NotifyUserJoined(Player player)
        {
            Players.Add(new PlayerGameState { Player = player });
            return Notifier.UserJoined(this.GameId, player);
        }

        public Task NotifyUserLeft(Player player)
        {
            return Notifier.UserLeft(this.GameId.ToString(), player);
        }

        #endregion
    }
}