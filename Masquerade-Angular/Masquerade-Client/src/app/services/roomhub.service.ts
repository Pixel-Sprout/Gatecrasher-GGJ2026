import { Injectable, inject} from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { EndpointLocator } from './EndpointLocator.service';
export interface GameRoom{
  gameId: string,
  gameName: string,
  currentPhase: string
}

@Injectable({ providedIn: 'root' })
export class RoomHubService {
  private locator: EndpointLocator = inject(EndpointLocator);
  private connection?: HubConnection;
  public playerName: string = '';
  private userToken: string = crypto.randomUUID();

  async connect(username: string, userToken: string | null = null): Promise<boolean> {
    if (this.connection && this.connection.state !== 'Disconnected') {
      return false;
    }

    if (userToken){
      this.userToken = userToken;
    }

    var hubUrl = `${this.locator.getSignalRHubEndpoint().replace(/\/$/, '')}/hubs/rooms?username=${encodeURIComponent(username)}&userToken=${encodeURIComponent(this.userToken)}`;

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    try{
    await this.connection.start();
    } catch (error) {
      Promise.reject("Could not connect to server");
      console.error('Error starting connection:', error);
      return false;
    }
    return true;
  }


  async disconnect(): Promise<void> {
    if (!this.connection) return;
    await this.connection.stop();
  }
}
