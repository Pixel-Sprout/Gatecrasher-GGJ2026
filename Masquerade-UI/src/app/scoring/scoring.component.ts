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
  isEvil: boolean;
  imageData: string;
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
  // cursor-bound preview state
  public previewVisible = false;
  public previewSrc: string | null = null;
  public previewX = 0;
  public previewY = 0;
 
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
      isEvil: !!p.isEvil,
      imageData: p.encodedMask
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

  showPreview(evt: MouseEvent, src: string): void {
    this.previewSrc = src;
    this.previewVisible = true;
    this.movePreview(evt);
  }

  movePreview(evt: MouseEvent): void {
    const padding = 12;
    const previewWidth = Math.min(window.innerWidth * 0.6, 320);
    const previewHeight = Math.min(window.innerHeight * 0.6, 320);

    // Prefer to the right and slightly below the cursor
    let x = evt.clientX + padding;
    if (x + previewWidth + padding > window.innerWidth) {
      x = evt.clientX - previewWidth - padding;
      if (x < padding) x = padding;
    }

    let y = evt.clientY + padding;
    if (y + previewHeight + padding > window.innerHeight) {
      y = evt.clientY - previewHeight - padding;
      if (y < padding) y = padding;
    }

    this.previewX = Math.round(x);
    this.previewY = Math.round(y);
  }

  hidePreview(): void {
    this.previewVisible = false;
    this.previewSrc = null;
  }
}