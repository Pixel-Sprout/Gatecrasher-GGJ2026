import { Injectable, signal, inject, effect } from '@angular/core';
import { BehaviorSubject} from "rxjs"
import { GameState } from '../types/game-state.enum';
@Injectable({
  providedIn: 'root'
})
export class AppStateService {
  private currentStateSignal = signal<GameState>(GameState.USER_SELECT);
  public currentStateSource = new BehaviorSubject<GameState>(GameState.USER_SELECT);


  //Messages
  public lobbyMessageSignal= signal<any>('');
  public drawingMessageSignal = signal<any>('');
  public votingMessageSignal = signal<any>('');
  public scoringMessageSignal = signal<any>('');
  public cutsceneMessageSignal = signal<any>('');

  public readonly currentState = this.currentStateSignal.asReadonly();

  constructor() {
    this.currentStateSource.subscribe((state) => {
      this.currentStateSignal.set(state);
    })
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
      case GameState.CUTSCENE_THE_CHOICE:
        this.cutsceneMessageSignal.set(message || '');
        break;
    }
    this.currentStateSource.next(state);
  }

  getState(): GameState {
    return this.currentStateSignal();
  }
}

