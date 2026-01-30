// ...existing code...
import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';

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

  onUserJoined(): Observable<UserEvent> {
    return this.userJoined$.asObservable();
  }

  onUserLeft(): Observable<UserEvent> {
    return this.userLeft$.asObservable();
  }

  onReceiveMessage(): Observable<string> {
    return this.receiveMessage$.asObservable();
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
// ...existing code...
