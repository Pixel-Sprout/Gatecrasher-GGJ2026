import { Injectable, signal, inject, effect } from '@angular/core';
import { GameState } from '../types/game-state.enum';
import { SignalrService } from './signalr.service';

@Injectable({
  providedIn: 'root'
})
export class AppStateService {
  private currentStateSignal = signal<GameState>(GameState.LOBBY);
  private signalrService = inject(SignalrService);
  
  //Messages
  public lobbyMessageSignal= signal<any>('');
  public drawingMessageSignal = signal<any>('');
  public votingMessageSignal = signal<any>('');
  public scoringMessageSignal = signal<any>('');

  public readonly currentState = this.currentStateSignal.asReadonly();

  constructor() {
    // Subscribe to SignalR game state changes
    this.signalrService.onGameStateChanged().subscribe(state => {
      this.setState(state, null);
    });
  }

  setState(state: GameState, message: any): void {
    switch(state) {
      case GameState.LOBBY:
        this.lobbyMessageSignal.set(message || '');
        break;
      case GameState.MASK_DRAW:
        this.drawingMessageSignal.set(message || '');
        break;
      case GameState.MASK_COMPARISON:
        this.votingMessageSignal.set(message || '');
        break;
      case GameState.SCORING:
        this.scoringMessageSignal.set(message || '');
        break;
    }
    this.currentStateSignal.set(state);
  }

  getState(): GameState {
    return this.currentStateSignal();
  }
}

