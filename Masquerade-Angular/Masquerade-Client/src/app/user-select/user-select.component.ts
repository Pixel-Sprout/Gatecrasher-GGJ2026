import {Component, inject, signal, OnInit, OnDestroy, Inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {BehaviorSubject, Subscription} from 'rxjs';
import {GameHubService, GameRoom} from '../services/gamehub.service';
import {AppStateService} from '../services/app-state.service';
import {GameState} from '../types/game-state.enum';
import { WINDOW } from '../window.provider';

@Component({
  selector: 'app-user-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-select.component.html',
  styleUrls: ['./user-select.component.scss']
})
export class UserSelectComponent implements OnInit, OnDestroy {
  private svc = inject(GameHubService);
  protected readonly appState = inject(AppStateService);

  roomsSource = new BehaviorSubject<GameRoom[]>([]);
  rooms = this.roomsSource.asObservable();
  userName = '';
  connected = signal<boolean>(false);
  newRoomName = '';
  errorMessage = signal<string | null>(null);
  requestedRoomId: string = "";

  private subs = new Subscription();

  constructor(@Inject(WINDOW) private window: Window) {
    this.requestedRoomId = window.location.search.substring(1).trim();
    console.log(this.requestedRoomId);
  }
  ngOnInit(): void {
    const s = this.svc.receiveGameRooms$.subscribe(rooms => {
      var reqestedRoom = rooms.find(r => r.gameId == this.requestedRoomId);
      if(reqestedRoom){
        console.log("got room", reqestedRoom);
        this.joinRoom(reqestedRoom);
      }

      this.roomsSource.next(rooms);
    });
    this.subs.add(s);

    this.svc.onReceivePhaseChanged().subscribe(([phase, message]) =>
      setTimeout(() => {
        this.appState.setState(phase as GameState, message);
      }, 800)
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  connect() {
    if (!this.userName.trim()) {
      return;
    }

    this.svc.connect(this.userName).then((success) => {
      if(success) {
        this.errorMessage.set(null);
        // Wywołanie na serwerze powinno spowodować, że receiveGameRooms$ wyemituje listę
        this.svc.getAvailableGameRooms().catch(() => {});
        this.connected.set(true);
      }else{
        this.errorMessage.set('Error connecting to server.');
      }
    });
  }

  disconnect() {
    this.connected.set(false);

    this.errorMessage.set(null);

    this.svc.disconnect().then(() => {
      console.log(this.userName + ' disconnected from the game hub.');
    });
  }

  joinRoom(room: GameRoom) {
    if (!this.connected) return;

    this.svc.joinGame(room.gameId)
  }

  createRoom() {
    if (!this.connected) return;
    const name = (this.newRoomName || '').trim();
    if (!name) return;
    this.svc.CreateAndJoinGame(name);
  }

  trackByGameId(index: number, room: GameRoom) {
    return room?.gameId ?? index;
  }
}
