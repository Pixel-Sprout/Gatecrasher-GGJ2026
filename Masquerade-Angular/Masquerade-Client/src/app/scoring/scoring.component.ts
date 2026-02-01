import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppStateService } from '../services/app-state.service';
import { GameState } from '../types/game-state.enum';
import { GameHubService } from '../services/gamehub.service';

interface ScoringPlayer {
  id: string;
  name: string;
  role: string;
  score: number;
  selectedDifferentMaskId?: string | null;
  isReady: boolean;
  approved?: boolean; // whether they chose the "different" mask (for demo)
}

@Component({
  selector: 'app-scoring',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './scoring.component.html',
  styleUrl: './scoring.component.scss'
})
export class ScoringComponent implements OnInit {
  private appState = inject(AppStateService);
  private svc = inject(GameHubService);
  public players = signal<ScoringPlayer[]>([]);


  get filteredPlayers(): ScoringPlayer[] {
    return this.players().filter(p => p.approved);
  }
 
  ngOnInit(): void {
    this.svc.onReceivePhaseChanged().subscribe(([phase, message]) => 
      setTimeout(() => {
        this.appState.setState(phase as GameState, message);
      }, 800)
    );

    this.svc.onReceivePlayersInTheRoom().subscribe(msg => {
      this.players().forEach(p => p.isReady = msg.filter(m => m.userId == p.id)[0].isReady) 
      this.scoreboard; }
    );

    this.appState.scoringMessageSignal().players.forEach((p: any) => {
      this.players().push({
        id: p.player.userId,
        name: p.player.username,
        role: 'Role 1',
        score: p.score,
        selectedDifferentMaskId: p.votedPlayerId,
        isReady: p.player.isReady,
        approved: p.votedPlayerId === null ? true : p.votedPlayerId === p.player.userId // for demo purposes
        });
      }
    );
  }

  // compute sorted scoreboard
  get scoreboard(): ScoringPlayer[] {
    return [...this.players()].sort((a, b) => b.score - a.score);
  }

  // mark current player as not ready and go to lobby immediately (Cancel)
  toggleReady(): void {
    this.svc.ready();
  }
}