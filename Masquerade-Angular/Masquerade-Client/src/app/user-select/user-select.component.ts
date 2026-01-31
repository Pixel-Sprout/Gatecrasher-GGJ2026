import {Component, inject, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {Subscription} from 'rxjs';
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
  errorMessage = signal<string | null>(null);

  // subskrypcja do listy pokojów (wycofywana przy disconnect/reset)
  private roomsSub: Subscription | null = null;

  connect() {
    if (!this.userName.trim()) {
      return;
    }

    this.svc.receiveGameRooms$.subscribe(room => {
      this.rooms.set(room);
    });

    this.svc.connect(this.userName).then((success) => {
      if(success) {
        this.errorMessage.set(null);
        console.log(this.userName + ' connected to the game hub.', success);
        this.svc.getAvailableGameRooms().finally();
        this.connected = true;
      }else{
        this.errorMessage.set('Error connecting to server.');
      }
    }, (error) => {
      this.errorMessage.set('Błąd połączenia: ' + (error?.message ?? String(error)));
      console.error('Error connecting to game hub:', error);
    });
  }

  disconnect() {
    this.connected = false;

    this.errorMessage.set(null);

    // odsubskrybuj listę pokojów
    this.roomsSub?.unsubscribe();
    this.roomsSub = null;

    this.svc.disconnect().then(() => {
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
    this.svc.CreateAndJoinGame(name);
    this.appState.setState(GameState.LOBBY, "");
  }

  reset() {
    this.userName = '';
    this.connected = false;
    this.errorMessage.set(null);

    // odsubskrybuj też przy resecie
    this.roomsSub?.unsubscribe();
    this.roomsSub = null;
  }
}
