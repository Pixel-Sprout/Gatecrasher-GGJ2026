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
      // Other players are already ready except the current player
      const isReady = p.id !== this.currentPlayerId;
      return {
        id: p.id,
        name: p.name,
        role: p.role,
        score: randomScore,
        selectedDifferentMaskId: approved ? 'someMaskId' : null,
        hasReady: isReady,
        approved
      } as ScoringPlayer;
    });
  }

  // Toggle ready state for current user
  toggleReady(): void {
    const me = this.players.find(p => p.id === this.currentPlayerId);
    if (!me) return;
    me.hasReady = !me.hasReady;

    // if everyone is ready, navigate to mask-creator with a new feature set
    if (this.allReady && !this.navigating) {
      this.navigating = true;
      const features = this.generateNewFeatureSections();
      setTimeout(() => {
        this.router.navigate(['/mask-creator'], { state: { featureSections: features } });
      }, 800);
    }
  }

  // For demo: increment score (not required, but useful for testing)
  addPoints(playerId: string, pts = 1): void {
    const p = this.players.find(x => x.id === playerId);
    if (p) p.score += pts;
  }

  // Generate a new set of feature requirements for the next round
  private generateNewFeatureSections(): { name: string; description: string }[] {
    const pick = <T,>(arr: T[]) => arr[Math.floor(Math.random() * arr.length)];

    const eyes = ['Duże', 'Małe', 'Migdałowe', 'Wąskie'];
    const mouths = ['Szerokie', 'Wąskie', 'Uśmiechnięte', 'Zamknięte'];
    const noses = ['Duży', 'Mały', 'Prosty', 'Haczykowaty'];
    const beards = ['Gęsty', 'Rzadki', 'Brak', 'Krótki'];
    const ears = ['Szpiczaste', 'Zaokrąglone', 'Duże', 'Małe'];

    return [
      { name: 'Oczy', description: pick(eyes) },
      { name: 'Usta', description: pick(mouths) },
      { name: 'Nos', description: pick(noses) },
      { name: 'Zarost', description: pick(beards) },
      { name: 'Uszy', description: pick(ears) }
    ];
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