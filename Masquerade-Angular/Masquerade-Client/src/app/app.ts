import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LobbyComponent } from './lobby/lobby.component';
import { MaskCreatorComponent } from './mask-creator/mask-creator.component';
import { MaskComparisonComponent } from './mask-comparison/mask-comparison.component';
import { ScoringComponent } from './mask-comparison/scoring/scoring.component';
import { TheBallroomComponent } from './the-ballroom/the-ballroom.component';
import { AppStateService } from './services/app-state.service';
import { GameState } from './types/game-state.enum';
import {UserSelectComponent} from './user-select/user-select.component';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    LobbyComponent,
    MaskCreatorComponent,
    MaskComparisonComponent,
    ScoringComponent,
    TheBallroomComponent,
    UserSelectComponent
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('Masquerade-Client');
  protected readonly appState = inject(AppStateService);
  protected readonly GameState = GameState;
}
