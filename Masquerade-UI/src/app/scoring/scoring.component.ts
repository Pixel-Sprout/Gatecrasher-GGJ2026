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
  guessedRight: boolean;
  isEvil: boolean
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
 
  ngOnInit(): void {
    this.svc.onReceivePhaseChanged().subscribe(([phase, message]) => 
      setTimeout(() => {
        this.appState.setState(phase as GameState, message);
      }, 800)
    );

    // Update players' ready state immutably so the signal notifies consumers
    this.svc.onReceivePlayersInTheRoom().subscribe(msg => {
      const updated = this.players().map(p => {
        const found = msg.find((m: any) => m.userId === p.id);
        return { ...p, isReady: found ? !!found.isReady : p.isReady };
      });
      this.players.set(updated);
    });

    const evilPlayerId = this.appState.scoringMessageSignal().players.find((p: any) => p.isEvil)?.player.userId;

    // Populate `players` signal immutably from the scoring message
    const initialPlayers = this.appState.scoringMessageSignal().players.map((p: any, idx: number) => ({
      id: p.player.userId,
      name: p.player.username,
      role: idx === 0 ? 'Admin' : 'Mask Maker',
      score: p.score,
      selectedDifferentMaskId: p.votedPlayerId,
      isReady: !!p.player.isReady,
      guessedRight: p.votedPlayerId === evilPlayerId,
      isEvil: !!p.isEvil
    }));
    this.players.set(initialPlayers);
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