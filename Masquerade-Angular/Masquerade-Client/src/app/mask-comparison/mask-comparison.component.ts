import { Component, OnInit, OnDestroy, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppStateService } from '../services/app-state.service';
import { GameState } from '../types/game-state.enum';
import { GameHubService } from '../services/gamehub.service';

interface PlayerMask {
  id: string;
  playerName: string;
  playerRole: string;
  imageData: string;
}

interface VotingPlayer {
  id: string;
  name?: string | null;
  role: string;
  hasVoted: boolean;
}

@Component({
  selector: 'app-mask-comparison',
  imports: [CommonModule],
  templateUrl: './mask-comparison.component.html',
  styleUrl: './mask-comparison.component.scss',
  standalone: true
})
export class MaskComparisonComponent implements OnInit, OnDestroy {
  playerMasks: PlayerMask[] = [];
  votingPlayers = signal<VotingPlayer[]>([]);
  selectedMaskId: string | null = null;

  // Timer state for deadline progress
  deadline: Date | null = null;
  totalSeconds: number = 60; // fallback duration
  remainingSeconds = signal<number>(0);
  deadlineProgress = signal<number>(0);
  private timerInterval: any = null;

  get votedCount(): number {
    return this.votingPlayers().filter(p => p.hasVoted).length;
  }

  get progressPercentage(): number {
    if (this.votingPlayers().length === 0) return 0;
    return (this.votedCount / this.votingPlayers().length) * 100;
  }

  private appState = inject(AppStateService);
  private svc = inject(GameHubService);

  ngOnInit(): void {
    this.loadPlayerMasks();
    
    // read deadline from voting message if present
    try {
      const msg = this.appState.votingMessageSignal();
      if (msg && msg.phaseEndsAt) {
        this.setDeadlineFromValue(msg.phaseEndsAt, Date.now());
      }
    } catch(e) {}

    this.startTimer();

    this.svc.onReceivePhaseChanged().subscribe(([phase, message]) => 
      setTimeout(() => {
        this.appState.setState(phase as GameState, message);
      }, 800)
    );

    this.svc.onReceivePlayersInTheRoom().subscribe(msg =>
      this.votingPlayers.set(msg.map((player, i) => ({ id: player.userId, name: player.username, role: 'Mask Maker', hasVoted: player.isReady })))
    );


  }

  ngOnDestroy(): void {
    this.stopTimer();
  }

  private setDeadlineFromValue(deadlineVal: any, startedAt?: any): void {
    try {
      const dl = new Date(deadlineVal);
      if (isNaN(dl.getTime())) return;
      this.deadline = dl;
      if (startedAt) {
        const st = new Date(startedAt);
        if (!isNaN(st.getTime())) {
          const total = Math.max(1, Math.round((dl.getTime() - st.getTime()) / 1000));
          this.totalSeconds = total;
        }
      }
    } catch (e) {
      // ignore
    }
  }

  private startTimer(): void {
    this.stopTimer();
    if (!this.deadline) return;
    this.updateTimer();
    this.timerInterval = setInterval(() => this.updateTimer(), 1000);
  }

  private updateTimer(): void {
    if (!this.deadline) return;
    const now = Date.now();
    const remainingMs = this.deadline.getTime() - now;
    this.remainingSeconds.set(remainingMs / 1000);
    const elapsed = this.totalSeconds - this.remainingSeconds();
    this.deadlineProgress.set((elapsed / this.totalSeconds) * 100);
    if (this.remainingSeconds() <= 0) {
      this.stopTimer();
    }
  }

  private stopTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }

  private loadPlayerMasks(): void {
    let placeholder = '';
    try {
      const heartSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="#e74c3c" d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/></svg>`;
      placeholder = 'data:image/svg+xml;base64,' + btoa(heartSvg);
    } catch (e) {
      // fallback small transparent pixel
      placeholder = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
    }

    this.appState.votingMessageSignal().masks.forEach((m: any) => {
      this.playerMasks.push({
        id: m.player.userId,
        playerName: m.player.username,
        playerRole: 'Role 1',
        imageData: m.encodedMask
        });
      this.votingPlayers().push({
        id: m.player.userId,
        name: m.player.username,
        role: 'Role 1',
        hasVoted: m.player.isReady
        });
    });
  }

  selectMask(maskId: string): void {
    this.selectedMaskId = maskId;
  }

  toggleReady(): void {
    if (!this.selectedMaskId) return;

    console.log('Vote submitted for mask:', this.selectedMaskId);

    this.svc.castVote(this.selectedMaskId);
    this.svc.ready();
  }
}
