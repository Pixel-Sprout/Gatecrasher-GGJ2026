import { Routes } from '@angular/router';
import { LobbyComponent } from '../lobby/lobby.component';
import { MaskCreatorComponent } from '../mask-creator/mask-creator.component';
import { MaskComparisonComponent } from '../mask-comparison/mask-comparison.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'lobby',
    pathMatch: 'full'
  },
  {
    path: 'lobby',
    component: LobbyComponent
  },
  {
    path: 'mask-creator',
    component: MaskCreatorComponent
  },
  {
    path: 'mask-comparison',
    component: MaskComparisonComponent
  }

  
];