import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { GameState } from '../types/game-state.enum';

export interface UserEvent {
  connectionId: string;
  username?: string | null;
}

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private connection?: HubConnection;
  private userJoined$ = new Subject<UserEvent>();
  private userLeft$ = new Subject<UserEvent>();
  private receiveMessage$ = new Subject<string>();
  private gameStateChanged$ = new Subject<GameState>();

  onUserJoined(): Observable<UserEvent> {
    return this.userJoined$.asObservable();
  }

  onUserLeft(): Observable<UserEvent> {
    return this.userLeft$.asObservable();
  }

  onReceiveMessage(): Observable<string> {
    return this.receiveMessage$.asObservable();
  }

  onGameStateChanged(): Observable<GameState> {
    return this.gameStateChanged$.asObservable();
  }

  async connect(baseUrl: string, username: string): Promise<void> {
    if (this.connection && this.connection.state !== 'Disconnected') {
      return;
    }

    const hubUrl = `${baseUrl.replace(/\/$/, '')}/hubs/echo?username=${encodeURIComponent(username)}`;

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.connection.on('UserJoined', (connectionId: string, name?: string) => {
      this.userJoined$.next({ connectionId, username: name });
    });

    this.connection.on('UserLeft', (connectionId: string, name?: string) => {
      this.userLeft$.next({ connectionId, username: name });
    });

    this.connection.on('ReceiveMessage', (message: string) => {
      this.receiveMessage$.next(message);
    });

    this.connection.on('GameStateChanged', (state: string) => {
      const gameState = Object.values(GameState).includes(state as GameState)
        ? (state as GameState)
        : GameState.LOBBY;
      this.gameStateChanged$.next(gameState);
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
}

