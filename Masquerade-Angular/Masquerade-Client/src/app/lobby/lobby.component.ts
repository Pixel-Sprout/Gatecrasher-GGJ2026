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

interface LobbySettings {
  drawingTimeSeconds: number;
  votingTimeSeconds: number;
  rounds: number;
  totalNumberOfRequirements: number;
  goodPlayerNumberOfRequirements: number;
  badPlayerNumberOfRequirements: number;
  useLongMaskDescriptions: boolean;
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
  public currentPlayerId = 'player1';
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
      this.players.set(msg.map((player, i) => ({ id: player.userId, name: player.username, role: msg[0].userId == player.userId ? 'Admin' : 'Mask Maker', ready: player.isReady }))
      ));
    this.svc.onReceivePhaseChanged().subscribe(([phase, message]) =>
      setTimeout(() => {
        this.appState.setState(phase as GameState, message);
      }, 800)
    );
    this.svc.onReceiveGameSettingsUpdated().subscribe((settings) => {
      this.applySettings(settings);
    });

    this.appState.lobbyMessageSignal().players.forEach((p: any) => {
        this.players().push({
          id: p.userId,
          name: p.username,
          role: this.appState.lobbyMessageSignal().players[0] == p ? 'Admin' : 'Mask Maker',
          ready: p.isReady,
        });
      }
    );

    this.applySettings(this.appState.lobbyMessageSignal().settings);
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

  kickPlayer(playerId: string): void {
    // Only allow the admin (first player) to kick others
    if (!this.players() || this.players().length === 0) return;
    if (this.players()[0].id !== this.currentPlayerId) return;
    if (playerId === this.currentPlayerId) return;
    this.svc.kickPlayer(playerId).catch(err => console.error('Kick failed', err));
  }

  // Apply settings received from backend (call this from your subscription)
  applySettings(settings: LobbySettings) {
    if (!settings) return;
    this.timeToDraw.set(settings.drawingTimeSeconds ?? this.timeToDraw());
    this.timeToVote.set(settings.votingTimeSeconds ?? this.timeToVote());
    this.rounds.set(settings.rounds ?? this.rounds());
    this.totalNumberOfRequirements.set(settings.totalNumberOfRequirements ?? this.totalNumberOfRequirements());
    this.goodPlayerNumberOfRequirements.set(settings.goodPlayerNumberOfRequirements ?? this.goodPlayerNumberOfRequirements());
    this.badPlayerNumberOfRequirements.set(settings.badPlayerNumberOfRequirements ?? this.badPlayerNumberOfRequirements());
    this.useLongMaskDescriptions.set(settings.useLongMaskDescriptions ?? this.useLongMaskDescriptions());
  }

  // Send current settings to backend (implement server handler)
  saveSettings(): void {
    const payload: LobbySettings = {
      drawingTimeSeconds: this.timeToDraw(),
      votingTimeSeconds: this.timeToVote(),
      rounds: this.rounds(),
      totalNumberOfRequirements: this.totalNumberOfRequirements(),
      goodPlayerNumberOfRequirements: this.goodPlayerNumberOfRequirements(),
      badPlayerNumberOfRequirements: this.badPlayerNumberOfRequirements(),
      useLongMaskDescriptions: this.useLongMaskDescriptions()
    };
    this.svc.updateGameSettings(payload);
    console.log('Lobby settings saved:', payload);
  }

  // New settings signals (defaults)
  public timeToDraw = signal<number>(60);
  public timeToVote = signal<number>(30);
  public rounds = signal<number>(3);
  public totalNumberOfRequirements = signal<number>(6);
  public goodPlayerNumberOfRequirements = signal<number>(4);
  public badPlayerNumberOfRequirements = signal<number>(2);
  public useLongMaskDescriptions = signal<boolean>(false);
}
