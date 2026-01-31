import { Component, inject, Inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AppStateService } from '../services/app-state.service';
import { GameState } from '../types/game-state.enum';
import { RouterOutlet } from '@angular/router';
import { GameHubService } from '../services/gamehub.service';

interface Player {
  id: string;
  name?: string | null;
  role: string;
  ready: boolean;
}

@Component({
  selector: 'app-lobby',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lobby.component.html',
  styleUrl: './lobby.component.scss',
})
export class LobbyComponent implements OnInit {
  public players = signal<Player[]>([]);
  currentPlayerId = 'player1';
  currentPlayerReady = false;
  private appState = inject(AppStateService);
  private svc = inject(GameHubService);

  constructor(private router: Router) { 
  }

  ngOnInit(): void {
    this.svc.connect(this.currentPlayerId).then(() => {
      this.svc.onReceivePlayersInTheRoom().subscribe(msg => 
        this.players.set(msg.map((player, i) => ({ id: player.connectionId, name: player.username, role: 'Mask Maker', ready: player.isReady }))
      ));
      this.svc.onReceivePhaseChanged().subscribe(([phase, message]) => 
        setTimeout(() => {
          this.appState.setState(phase as GameState, message);
        }, 800)
      );
      this.svc.getAllGameIds();  
    });
  }

  toggleReady(): void {
    this.svc.ready();
  }

  // get allPlayersReady(): boolean {
  //   return this.players().length > 0 && this.players().every(p => p.ready);
  // }

  // get readyCount(): number {
  //   return this.players().filter(p => p.ready).length;
  // }

  leaveGame(): void {
    this.svc.leaveGame();
    console.log('Player left the game');
  }

  sendMessage(message: string): void {
    // TODO: Implement chat functionality
    console.log('Message sent:', message);
  }
}
