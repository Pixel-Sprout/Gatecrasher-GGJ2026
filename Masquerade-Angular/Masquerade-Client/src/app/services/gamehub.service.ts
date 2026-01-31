// ...existing code...
import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { GameState } from '../types/game-state.enum';

export interface UserEvent {
  connectionId: string;
  username?: string | null;
  isReady: boolean;
}

@Injectable({ providedIn: 'root' })
export class GameHubService {
  private baseUrl = 'https://localhost:44330';
  private username = 'Player';
  private connection?: HubConnection;
  private receiveMessage$ = new Subject<string>();
  private receivePlayersInTheRoom$ = new Subject<UserEvent[]>();
  private receivePhaseChanged$ = new Subject<[GameState, any]>();
  private gameId: string = '';

  onReceiveMessage(): Observable<string> {
    return this.receiveMessage$.asObservable();
  }

  onReceivePlayersInTheRoom(): Observable<UserEvent[]> {
    return this.receivePlayersInTheRoom$.asObservable();
  }

  onReceivePhaseChanged(): Observable<[GameState, any]> {
    return this.receivePhaseChanged$.asObservable();
  }

  async connect(username: string): Promise<void> {
    if (this.connection && this.connection.state !== 'Disconnected') {
      return;
    }

    const hubUrl = `${this.baseUrl.replace(/\/$/, '')}/hubs/game?username=${encodeURIComponent(username)}`;

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.connection.on('ReceiveMessage', (message: string) => {
      this.receiveMessage$.next(message);
    });

    this.connection.on('ReceiveAllGameIds', (ids: string[]) => {
      // Set first id as current gameId 
      if (ids.length > 0) {
        this.gameId = ids[0];
        this.joinGame();
      }
    });
    
    this.connection.on('PlayersInTheRoom', (players: UserEvent[]) => {
      this.receivePlayersInTheRoom$.next(players);
    });
    
    this.connection.on('PhaseChanged', (phase: GameState, message: any) => {
      this.receivePhaseChanged$.next([phase, message]);
    });

    await this.connection.start();
  }

  async sendMessage(message: string): Promise<void> {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('SendMessage', message);
  }

  async disconnect(): Promise<void> {
    if (!this.connection) return;
    await this.connection.stop();
  }

  async getAllGameIds() {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('GetAllGameIds');
  }

  async joinGame() {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('JoinGame', this.gameId);
  }

  async leaveGame() {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('LeaveGame');
  }

  async ready() {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('PlayerReady');
  }
}
