import { Component, inject, Inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AppStateService } from '../services/app-state.service';
import { GameState } from '../types/game-state.enum';
import { QRCodeComponent } from 'angularx-qrcode';
import { GameHubService } from '../services/gamehub.service';
import { EndpointLocator } from '../services/EndpointLocator.service';
import {WINDOW} from '../window.provider';

interface Player {
  id: string;
  name?: string | null;
  role: string;
  ready: boolean;
}

@Component({
  selector: 'app-lobby',
  standalone: true,
  imports: [CommonModule, QRCodeComponent],
  templateUrl: './lobby.component.html',
  styleUrl: './lobby.component.scss',
})
export class LobbyComponent implements OnInit {
  public players = signal<Player[]>([]);
  public joinUrl = signal<string>("")
  currentPlayerId = 'player1';
  currentPlayerReady = false;
  private appState = inject(AppStateService);
  private svc = inject(GameHubService);
  private locator = inject(EndpointLocator);

  constructor(@Inject(WINDOW) private window: Window) {
    this.currentPlayerId = this.svc.playerId;
  }

  ngOnInit(): void {
    console.log(this.svc.gameId);
    this.joinUrl.set(this.locator.getRoomJoinUrl(this.svc.gameId))
    this.svc.onReceivePlayersInTheRoom().subscribe(msg =>
      this.players.set(msg.map((player, i) => ({ id: player.connectionId, name: player.username, role: 'Mask Maker', ready: player.isReady }))
      ));
    this.svc.onReceivePhaseChanged().subscribe(([phase, message]) =>
      setTimeout(() => {
        this.appState.setState(phase as GameState, message);
      }, 800)
    );

    this.appState.lobbyMessageSignal().players.forEach((p: any) => {
        this.players().push({
          id: p.connectionId,
          name: p.username,
          role: 'Role 1',
          ready: p.isReady,
        });
      }
    );
  }

  toggleReady(): void {
    this.svc.ready();
  }

  leaveGame(): void {
    this.svc.leaveGame();
    console.log('Player left the game');
  }

  sendMessage(message: string): void {
    // TODO: Implement chat functionality
    console.log('Message sent:', message);
  }
  copyToClipboard(url: string) {
    this.window.navigator.clipboard.writeText(url);
  }
}
