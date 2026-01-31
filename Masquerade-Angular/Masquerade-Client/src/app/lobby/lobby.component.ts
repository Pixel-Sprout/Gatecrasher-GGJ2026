import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';

interface Player {
  id: string;
  name: string;
  role: string;
  ready: boolean;
}

@Component({
  selector: 'app-lobby',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './lobby.component.html',
  styleUrl: './lobby.component.scss'
})
export class LobbyComponent implements OnInit {
  players: Player[] = [];
  currentPlayerId = 'player1';
  currentPlayerReady = false;

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.initializePlayers();
  }

  private initializePlayers(): void {
    // Initialize demo players
    this.players = [
      { id: 'player1', name: 'You', role: 'Mask Maker', ready: false },
      { id: 'player2', name: 'Player 2', role: 'Mask Maker', ready: false },
      { id: 'player3', name: 'Player 3', role: 'Mask Maker', ready: false }
    ];
  }

  toggleReady(): void {
    const currentPlayer = this.players.find(p => p.id === this.currentPlayerId);
    if (currentPlayer) {
      currentPlayer.ready = !currentPlayer.ready;
      this.currentPlayerReady = currentPlayer.ready;
    }

    // If all players are ready, redirect to mask creator
    if (this.allPlayersReady) {
      setTimeout(() => {
        this.router.navigate(['/mask-creator']);
      }, 800);
    }
  }

  get allPlayersReady(): boolean {
    return this.players.length > 0 && this.players.every(p => p.ready);
  }

  get readyCount(): number {
    return this.players.filter(p => p.ready).length;
  }

  leaveGame(): void {
    // TODO: Handle leaving game
    console.log('Player left the game');
  }

  sendMessage(message: string): void {
    // TODO: Implement chat functionality
    console.log('Message sent:', message);
  }
}