import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

interface PlayerMask {
  id: string;
  playerName: string;
  playerRole: string;
  imageData: string;
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
  selectedMaskId: string | null = null;

  ngOnInit(): void {
    this.loadPlayerMasks();
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

  selectMask(maskId: string): void {
    this.selectedMaskId = maskId;
  }

  submitVote(): void {
    if (!this.selectedMaskId) return;

    console.log('Vote submitted for mask:', this.selectedMaskId);
    // TODO: Send vote to backend
    // TODO: Navigate to results or next view
  }

  skipVote(): void {
    console.log('Vote skipped');
    // TODO: Handle skip vote
    // TODO: Navigate to next view
  }
}