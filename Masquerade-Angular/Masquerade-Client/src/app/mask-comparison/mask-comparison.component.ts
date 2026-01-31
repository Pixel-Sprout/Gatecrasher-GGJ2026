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
    // TODO: Load masks from backend or service
    // This is sample data for demonstration
    this.playerMasks = [
      {
        id: 'player1',
        playerName: 'Player 1',
        playerRole: 'Role 1',
        imageData: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==' // placeholder
      },
      {
        id: 'player2',
        playerName: 'Player 2',
        playerRole: 'Role 2',
        imageData: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==' // placeholder
      },
      {
        id: 'player3',
        playerName: 'Player 3',
        playerRole: 'Role 1',
        imageData: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==' // placeholder
      }
    ];
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

    // If all players have voted, redirect to scoring
    if (this.allVoted) {
      setTimeout(() => {
        this.router.navigate(['/scoring']);
      }, 800);
    }
  }

  skipVote(): void {
    console.log('Vote skipped');
    
    // Mark current player as voted (even though they skipped)
    const currentPlayer = this.votingPlayers.find(p => p.id === this.currentPlayerId);
    if (currentPlayer) {
      currentPlayer.hasVoted = true;
    }

    // If all players have voted, redirect to scoring
    if (this.allVoted) {
      setTimeout(() => {
        this.router.navigate(['/scoring']);
      }, 800);
    }
  }

  get allVoted(): boolean {
    return this.votingPlayers.length > 0 && this.votingPlayers.every(p => p.hasVoted);
  }
}