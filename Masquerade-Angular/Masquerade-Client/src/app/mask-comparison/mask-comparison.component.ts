import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

interface PlayerMask {
  id: string;
  playerName: string;
  playerRole: string;
  imageData: string;
}

interface VotingPlayer {
  id: string;
  name: string;
  role: string;
  hasVoted: boolean;
}

@Component({
  selector: 'app-mask-comparison',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './mask-comparison.component.html',
  styleUrl: './mask-comparison.component.scss',
  standalone: true
})
export class MaskComparisonComponent implements OnInit {
  playerMasks: PlayerMask[] = [];
  votingPlayers: VotingPlayer[] = [];
  selectedMaskId: string | null = null;
  currentPlayerId = 'player1';

  get votedCount(): number {
    return this.votingPlayers.filter(p => p.hasVoted).length;
  }

  get progressPercentage(): number {
    if (this.votingPlayers.length === 0) return 0;
    return (this.votedCount / this.votingPlayers.length) * 100;
  }

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.loadPlayerMasks();
    this.initializeVotingPlayers();
  }

  private loadPlayerMasks(): void {
    // Try loading masks saved in localStorage (saved by mask-creator)
    try {
      const stored = JSON.parse(localStorage.getItem('masquerade_masks') || '[]');
      if (Array.isArray(stored) && stored.length > 0) {
        this.playerMasks = stored.map((m: any) => ({
          id: m.id,
          playerName: m.playerName || 'Player',
          playerRole: m.playerRole || 'Mask Maker',
          imageData: m.imageData
        } as PlayerMask));
      } else {
        this.playerMasks = [];
      }
    } catch (e) {
      console.warn('Could not read masks from localStorage', e);
      this.playerMasks = [];
    }

    // Ensure we have placeholders for other players if needed
    // Generate a base64 SVG heart placeholder at runtime (fallback to 1x1 PNG if btoa isn't available)
    let placeholder = '';
    try {
      const heartSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="#e74c3c" d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/></svg>`;
      placeholder = 'data:image/svg+xml;base64,' + btoa(heartSvg);
    } catch (e) {
      // fallback small transparent pixel
      placeholder = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
    }
    const requiredIds = ['player1', 'player2', 'player3'];
    requiredIds.forEach((id, idx) => {
      if (!this.playerMasks.find(p => p.id === id)) {
        this.playerMasks.push({
          id,
          playerName: id === 'player1' ? 'You' : `Player ${idx + 1}`,
          playerRole: 'Role 1',
          imageData: placeholder
        });
      }
    });
  }

  private initializeVotingPlayers(): void {
    // TODO: Load from backend or service
    this.votingPlayers = [
      {
        id: 'player1',
        name: 'Player 1',
        role: 'Role 1',
        hasVoted: false
      },
      {
        id: 'player2',
        name: 'Player 2',
        role: 'Role 2',
        hasVoted: false
      },
      {
        id: 'player3',
        name: 'Player 3',
        role: 'Role 1',
        hasVoted: false
      }
    ];
  }

  selectMask(maskId: string): void {
    this.selectedMaskId = maskId;
  }

  submitVote(): void {
    if (!this.selectedMaskId) return;

    console.log('Vote submitted for mask:', this.selectedMaskId);
    
    // Mark current player as voted
    const currentPlayer = this.votingPlayers.find(p => p.id === this.currentPlayerId);
    if (currentPlayer) {
      currentPlayer.hasVoted = true;
    }

    // Navigate to scoring after submitting this user's vote
    // (the backend/game server would normally decide when to show results;
    // for now redirect the submitting user immediately)
    setTimeout(() => {
      this.router.navigate(['/scoring']);
    }, 300);
  }

  skipVote(): void {
    console.log('Vote skipped');
    
    // Mark current player as voted (even though they skipped)
    const currentPlayer = this.votingPlayers.find(p => p.id === this.currentPlayerId);
    if (currentPlayer) {
      currentPlayer.hasVoted = true;
    }

    // Redirect to scoring for the current user after skipping
    setTimeout(() => {
      this.router.navigate(['/scoring']);
    }, 300);
  }

  get allVoted(): boolean {
    return this.votingPlayers.length > 0 && this.votingPlayers.every(p => p.hasVoted);
  }
}