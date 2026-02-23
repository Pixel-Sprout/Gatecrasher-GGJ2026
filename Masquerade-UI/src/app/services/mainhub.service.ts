import {inject, Injectable} from '@angular/core';
import {EndpointLocator} from './EndpointLocator.service';
import {HubConnection, HubConnectionBuilder, LogLevel} from '@microsoft/signalr';
import {Subject} from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class MainHubService {
  private locator: EndpointLocator = inject(EndpointLocator);
  private connection?: HubConnection;

  public playerDataReceived$ = new Subject<[string, string | null]>();

  public async connect(token: string, roomToJoin: string | null = null): Promise<boolean> {
    var hubUrl = `${this.locator.getSignalRHubEndpoint().replace(/\/$/, '')}/hubs/main?userToken=${encodeURIComponent(token)}`;

    if(roomToJoin){
      hubUrl = hubUrl + `&roomToJoin=${encodeURIComponent(roomToJoin)}`;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    try{
      this.registerHandlers(this.connection);
      await this.connection.start();
    } catch (error) {
      Promise.reject("Could not connect to server");
      console.error('Error starting connection:', error);
      return false
    }
    return true;
  }

  private registerHandlers(connection: HubConnection) {
    connection.on('playerDataReceived', (playerName:string, roomId: string | null, rooms:any[]) => {
      this.playerDataReceived$.next([playerName, roomId]);
      console.log("available rooms", rooms);
    });
    connection.on('roomCreated', (room:any) => {
      console.log("room", room);
    });
  }

  public SetPlayerName(name: string) {
    if (!this.connection) {
      console.error('Connection not established');
      return;
    }
    this.connection.invoke('SetPlayerName', name).catch(err => console.error(err));
  }

  public CreateRoom(roomName: string) {
    if (!this.connection) {
      console.error('Connection not established');
      return;
    }
    this.connection.invoke('CreateRoom', roomName).catch(err => console.error(err));
  }
}
