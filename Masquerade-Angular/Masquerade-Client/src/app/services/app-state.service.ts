import { Injectable, signal, inject, effect } from '@angular/core';
import { GameState } from '../types/game-state.enum';
import { SignalrService } from './signalr.service';

@Injectable({
  providedIn: 'root'
})
export class AppStateService {
  private currentStateSignal = signal<GameState>(GameState.LOBBY);
  private signalrService = inject(SignalrService);

  public readonly currentState = this.currentStateSignal.asReadonly();

  constructor() {
    // Subscribe to SignalR game state changes
    this.signalrService.onGameStateChanged().subscribe(state => {
      this.setState(state);
    });
  }

  setState(state: GameState): void {
    this.currentStateSignal.set(state);
  }

  getState(): GameState {
    return this.currentStateSignal();
  }
}

