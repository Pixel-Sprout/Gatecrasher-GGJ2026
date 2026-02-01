// ...existing code...
import { Injectable, inject} from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { GameState } from '../types/game-state.enum';
import { EndpointLocator } from './EndpointLocator.service';

export interface UserEvent {
  connectionId: string;
  username?: string | null;
  isReady: boolean;
}

export interface GameRoom{
  gameId: string,
  gameName: string,
  currentPhase: string
}

@Injectable({ providedIn: 'root' })
export class GameHubService {
  private locator: EndpointLocator = inject(EndpointLocator);
  private connection?: HubConnection;
  private receiveMessage$ = new Subject<string>();
  private receivePlayersInTheRoom$ = new Subject<UserEvent[]>();
  private receivePhaseChanged$ = new Subject<[GameState, any]>();
  public receiveGameRooms$ = new Subject<GameRoom[]>();
  public playerId: string = '';
  public playerName: string = '';
  public gameId: string = '';
  private userToken: string = crypto.randomUUID();

  onReceiveMessage(): Observable<string> {
    return this.receiveMessage$.asObservable();
  }

  onReceivePlayersInTheRoom(): Observable<UserEvent[]> {
    return this.receivePlayersInTheRoom$.asObservable();
  }

  onReceivePhaseChanged(): Observable<[GameState, any]> {
    return this.receivePhaseChanged$.asObservable();
  }

  async connect(username: string, userToken: string | null = null): Promise<boolean> {
    if (this.connection && this.connection.state !== 'Disconnected') {
      return false;
    }

    if (userToken){
      this.userToken = userToken;
    }

    var hubUrl = `${this.locator.getSignalRHubEndpoint().replace(/\/$/, '')}/hubs/game?username=${encodeURIComponent(username)}&userToken=${encodeURIComponent(this.userToken)}`;

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.connection.on("PlayerState", (playerName: string, playerId:string) =>{
      this.playerId = playerId;
      this.playerName = playerName;
    })

    this.connection.on('ReceiveMessage', (message: string) => {
      this.receiveMessage$.next(message);
    });

    this.connection.on('ReceiveAllGameIds', (rooms: GameRoom[]) => {
      this.receiveGameRooms$.next(rooms);
    });

    this.connection.on('PlayersInTheRoom', (players: UserEvent[]) => {
      this.receivePlayersInTheRoom$.next(players);
    });

    this.connection.on('PhaseChanged', (phase: GameState, message: any) => {
      this.receivePhaseChanged$.next([phase, message]);
    });

    this.connection.on('ExceptionMessage', (message: string, stackTrace: string) => {
      console.warn('ExceptionMessage:', message, stackTrace);
    });

    this.connection.on("onUserConnected", (userToken: string) => {
      console.log("User connected with token: " + userToken);
    });

    try{
    await this.connection.start();
    } catch (error) {
      Promise.reject("Could not connect to server");
      console.error('Error starting connection:', error);
      return false;
    }
    return true;
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

  async getAvailableGameRooms(): Promise<Observable<GameRoom[]>> {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('GetAllGameIds');
    return this.receiveGameRooms$.asObservable();
  }

  async CreateAndJoinGame(gameName: string) {
    if (!this.connection) throw new Error('Not connected');
    const newGameId = await this.connection.invoke<string>('CreateAndJoinGame', gameName);
    this.gameId = newGameId;
  }

  async joinGame(gameId: string) {
    if (!this.connection) throw new Error('Not connected');
    this.gameId = gameId;
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

  async castVote(selectedPlayerId: string) {
    if (!this.connection) throw new Error('Not connected');
    await this.connection.invoke('CastVote', selectedPlayerId);
  }
}
