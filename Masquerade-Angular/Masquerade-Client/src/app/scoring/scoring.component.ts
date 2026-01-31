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
  hasReady: boolean;
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
      this.players().forEach(p => p.hasReady = msg.filter(m => m.connectionId == p.id)[0].isReady) 
      this.scoreboard; }
    );

    this.appState.scoringMessageSignal().players.forEach((p: any) => {
      this.players().push({
        id: p.player.connectionId,
        name: p.player.username,
        role: 'Role 1',
        score: p.score,
        selectedDifferentMaskId: p.votedPlayerId,
        hasReady: p.player.isReady,
        approved: p.votedPlayerId === null ? true : p.votedPlayerId === p.player.connectionId // for demo purposes
        });
        this.scoreboard
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