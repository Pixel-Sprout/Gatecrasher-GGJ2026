// ...existing code...
import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SignalrService } from '../services/signalr.service';

@Component({
  selector: 'app-signalr-test',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div style="padding:16px">
      <h2>SignalR Test</h2>
      <label>Server URL: <input [(ngModel)]="serverUrl"/></label><br/>
      <label>Username: <input [(ngModel)]="username"/></label><br/>
      <button (click)="connect()">Connect</button>
      <button (click)="disconnect()">Disconnect</button>
      <button (click)="send()">Send Message</button>
      <div style="margin-top:12px">
        <strong>Log:</strong>
        <pre style="height:200px;overflow:auto;border:1px solid #ccc;padding:8px">{{logs}}</pre>
      </div>
    </div>
  `
})
export class SignalrTestComponent {
  serverUrl = 'https://localhost:44330';
  username = 'angular';
  logs = '';

  constructor(private svc: SignalrService, private cdr: ChangeDetectorRef) {
    this.svc.onUserJoined().subscribe(evt => this.log(`UserJoined: ${evt.connectionId} ${evt.username}`));
    this.svc.onUserLeft().subscribe(evt => this.log(`UserLeft: ${evt.connectionId} ${evt.username}`));
    this.svc.onReceiveMessage().subscribe(msg => this.log(`ReceiveMessage: ${msg}`));
  }

  private log(msg: string) {
    this.logs += msg + '\n';
    this.cdr.detectChanges();
  }

  async connect() {
    try {
      await this.svc.connect(this.serverUrl, this.username);
      this.log('Connected');
    } catch (e) {
      this.log('Connect error: ' + e);
    }
  }

  async disconnect() {
    try {
      await this.svc.disconnect();
      this.log('Disconnected');
    } catch (e) {
      this.log('Disconnect error: ' + e);
    }
  }

  async send() {
    const text = prompt('Message', 'test from angular');
    if (!text) return;
    try {
      this.svc.sendMessage(text);
      this.log('Sent: ' + text + ' (reply after 15s)');
    } catch (e) {
      this.log('Send error: ' + e);
    }
  }
}
// ...existing code...
