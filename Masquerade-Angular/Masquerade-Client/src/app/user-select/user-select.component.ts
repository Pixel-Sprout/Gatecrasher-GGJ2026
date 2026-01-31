import {Component, inject, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {GameHubService, GameRoom} from '../services/gamehub.service';
import {AppStateService} from '../services/app-state.service';
import {GameState} from '../types/game-state.enum';

@Component({
  selector: 'app-user-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-select.component.html',
  styleUrls: ['./user-select.component.scss']
})
export class UserSelectComponent {
  private svc = inject(GameHubService);
  protected readonly appState = inject(AppStateService);

  rooms = signal<GameRoom[]>([]);
  userName = '';
  connected = false;
  newRoomName = '';

  connect() {
    if (!this.userName.trim()) {
      return;
    }
    this.connected = true;

    this.svc.receiveGameRooms$.subscribe(room => {
      this.rooms.set(room);
    });
    this.svc.connect(this.userName).then(() => {
      // log when connected
      console.log(this.userName + ' connected to the game hub.');
      this.svc.getAvailableGameRooms().finally();
    });
  }

  disconnect() {
    this.connected = false;

    this.svc.disconnect().then(() => {
      // log when disconnected
      console.log(this.userName + ' disconnected from the game hub.');
    });
  }

  joinRoom(room: GameRoom) {
    if (!this.connected) return;

    this.svc.joinGame(room.gameId)
      .then(() =>{
        this.appState.setState(GameState.LOBBY, "");
      });
  }

  createRoom() {
    if (!this.connected) return;
    const name = (this.newRoomName || '').trim();
    if (!name) return;
    //const newRoom: GameRoom = {
    //  id: Date.now().toString(),
    //  name,
    //  status: 'open'
    //};
    //this.rooms.unshift(newRoom);
    //this.newRoomName = '';
    console.log(this.userName + ' created room ' + name);
    // TODO: call service to create room on server / SignalR
  }

  reset() {
    this.userName = '';
    this.connected = false;
  }
}
