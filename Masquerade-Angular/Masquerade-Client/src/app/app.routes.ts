import { Routes } from '@angular/router';
import { LobbyComponent } from './lobby/lobby.component';
import { MaskCreatorComponent } from './mask-creator/mask-creator.component';
import { MaskComparisonComponent } from './mask-comparison/mask-comparison.component';
import { ScoringComponent } from './mask-comparison/scoring/scoring.component';

export const routes: Routes = [
  /*{
    path: '',
    redirectTo: '',
    pathMatch: 'full'
  },
  // {
  //   path: 'lobby',
  //   component: LobbyComponent
  // },
  // {
  //   path: 'mask-creator',
  //   component: MaskCreatorComponent
  // },
  // {
  //   path: 'mask-comparison',
  //   component: MaskComparisonComponent
  // },
  // {
  //   path: 'scoring',
  //   component: ScoringComponent
  // },
  { path: 'signalr-test', loadComponent: () => import('./signalr-test/signalr-test.component').then(m => m.SignalrTestComponent) },
  { path: 'game-hub-test', loadComponent: () => import('./pages/game-hub-test/game-hub-test.component').then(m => m.GameHubTestComponent) },
  //{ path: '', pathMatch: 'full', redirectTo: 'signalr-test' }
];
