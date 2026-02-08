using Masquerade_GGJ_2026.Models.Enums;

namespace Masquerade_GGJ_2026.Models
{
    public class PhaseDetails
    {
        public RoundPhase CurrentPhase { get; set; }
        public DateTime? PhaseEndsAt { get; set; }
        public CancellationTokenSource? PhaseCts { get; set; }

        public PhaseDetails()
        {
            CurrentPhase = RoundPhase.UserSelect;
        }

        public void NextPhase()
        {
            switch(CurrentPhase)
            {
                case RoundPhase.UserSelect:
                    CurrentPhase = RoundPhase.Lobby;
                    break;
                case RoundPhase.Lobby:
                    CurrentPhase = RoundPhase.CutsceneOpening;
                    break;
                case RoundPhase.CutsceneOpening:
                    CurrentPhase = RoundPhase.Drawing;
                    break;
                case RoundPhase.Drawing:
                    CurrentPhase = RoundPhase.CutsceneMakeTheMask;
                    break;
                case RoundPhase.CutsceneMakeTheMask:
                    CurrentPhase = RoundPhase.Voting;
                    break;
                case RoundPhase.Voting:
                    CurrentPhase = RoundPhase.CutsceneTheChoice;
                    break;
                case RoundPhase.CutsceneTheChoice:
                    CurrentPhase = RoundPhase.Scoreboard;
                    break;
                case RoundPhase.Scoreboard:
                    CurrentPhase = RoundPhase.Lobby;
                    break;
            }
        }
    }
}
