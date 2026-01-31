import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';

interface ScoringPlayer {
  id: string;
  name: string;
  role: string;
  score: number;
  selectedDifferentMaskId?: string | null;
  hasReady: boolean;
  approved?: boolean; // whether they chose the "different" mask (for demo)
}

@Component({
  selector: 'app-scoring',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './scoring.component.html',
  styleUrl: './scoring.component.scss'
})
export class ScoringComponent implements OnInit {
  private router = inject(Router); // inject 
  
  public players: ScoringPlayer[] = [];
  public currentPlayerId = 'player1'; // replace with real current player id from auth/service
  public navigating = false;


  get filteredPlayers(): ScoringPlayer[] {
    return this.players.filter(p => p.approved);
  }

  get playerReady(): ScoringPlayer {
    return this.players.find(p => p.id === this.currentPlayerId)!;
  }
  

  ngOnInit(): void {
    this.populateDemoPlayers();
  }

  private populateDemoPlayers(): void {
    // create demo players with random scores and random "approved" flag
    const demo = [
      { id: 'player1', name: 'You', role: 'Mask Maker' },
      { id: 'player2', name: 'Player 2', role: 'Mask Maker' },
      { id: 'player3', name: 'Player 3', role: 'Mask Maker' },
      { id: 'player4', name: 'Player 4', role: 'Mask Maker' }
    ];

    this.players = demo.map(p => {
      const randomScore = Math.floor(Math.random() * 20); // random integer
      const approved = Math.random() > 0.5;
      return {
        id: p.id,
        name: p.name,
        role: p.role,
        score: randomScore,
        selectedDifferentMaskId: approved ? 'someMaskId' : null,
        hasReady: false,
        approved
      } as ScoringPlayer;
    });
  }

  // Toggle ready state for current user
  toggleReady(): void {
    const me = this.players.find(p => p.id === this.currentPlayerId);
    if (!me) return;
    me.hasReady = !me.hasReady;

    // if everyone is ready, navigate back to lobby after a short delay
    if (this.allReady && !this.navigating) {
      this.navigating = true;
      setTimeout(() => {
        this.router.navigate(['/lobby']);
      }, 800);
    }
  }

  // For demo: increment score (not required, but useful for testing)
  addPoints(playerId: string, pts = 1): void {
    const p = this.players.find(x => x.id === playerId);
    if (p) p.score += pts;
  }

  get allReady(): boolean {
    return this.players.length > 0 && this.players.every(p => p.hasReady);
  }

  // compute sorted scoreboard
  get scoreboard(): ScoringPlayer[] {
    return [...this.players].sort((a, b) => b.score - a.score);
  }

  // mark current player as not ready and go to lobby immediately (Cancel)
  backToLobby(): void {
    this.router.navigate(['/lobby']);
  }
}