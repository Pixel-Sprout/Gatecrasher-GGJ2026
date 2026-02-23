import {Component, inject, signal, OnInit, OnDestroy, Inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {BehaviorSubject, Subscription} from 'rxjs';
import {GameHubService, GameRoom} from '../services/gamehub.service';
import {AppStateService} from '../services/app-state.service';
import {GameState} from '../types/game-state.enum';
import { WINDOW } from '../window.provider';
import {MainHubService} from '../services/mainhub.service';
import {LocalStorageService} from '../services/local-storage.service';

@Component({
  selector: 'app-user-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-select.component.html',
  styleUrls: ['./user-select.component.scss']
})
export class UserSelectComponent implements OnDestroy {
 // private svc = inject(GameHubService);
  private mainHub = inject(MainHubService);
  private storage = inject(LocalStorageService);
  protected readonly appState = inject(AppStateService);

  protected connected = signal<boolean>(false);
  protected nameSelected = signal<boolean>(false);
  protected playerName = signal<string>("");
  protected roomName = signal<string>("");
  protected errorMessage = signal<string | null>(null);



  roomsSource = new BehaviorSubject<GameRoom[]>([]);
  rooms = this.roomsSource.asObservable();

  private subs = new Subscription();

  constructor(@Inject(WINDOW) private window: Window) {
    this.registerMainHubHandlers();

    let requestedRoomId = window.location.search.substring(1).trim();
    this.storage.UserToken$.subscribe(token => {
      this.mainHub.connect(token, requestedRoomId).then((success) => {
        this.connected.set(success)
        if(!success){
          this.errorMessage.set('Error connecting to server.');
        }
      })
    });
  }

  private registerMainHubHandlers() {
    this.subs.add(this.mainHub.playerDataReceived$.subscribe(([playerName, roomId]) =>
      this.onPlayerDataReceived(playerName, roomId)));
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  setName() {
    if(!this.connected()){
      this.errorMessage.set('Not connected.');
      return;
    }

    this.mainHub.SetPlayerName(this.playerName().trim());
  }

  setRoomName() {
    this.mainHub.CreateRoom(this.roomName().trim());
  }

  private onPlayerDataReceived(playerName: string, roomId: string | null) {
    console.log('Received player data:', playerName, roomId);
    if(playerName.trim()){
      this.playerName.set(playerName.trim());
      this.nameSelected.set(true);
      this.errorMessage.set(null);
    }
  }
}
