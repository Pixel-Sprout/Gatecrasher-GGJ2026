namespace Masquerade_GGJ_2026.Models
{
    public class PhaseDetails
    {
        public RoundPhase CurrentPhase { get; set; }
        public DateTime? PhaseEndsAt { get; set; }
        public CancellationTokenSource? PhaseCts { get; set; }
        public int DrawingTimeSeconds { get; set; }
        public int VotingTimeSeconds { get; set; }

        public PhaseDetails(int drawingTimeSeconds, int votingTimeSeconds)
        {
            CurrentPhase = RoundPhase.Lobby;
            DrawingTimeSeconds = drawingTimeSeconds;
            VotingTimeSeconds = votingTimeSeconds;
        }

        public void NextPhase()
        {
            var newPhase = (int)(CurrentPhase + 1) % 4;
            CurrentPhase = (RoundPhase)newPhase;
        }
    }
}
