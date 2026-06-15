import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'informes',
    canActivate: [authGuard],
    loadComponent: () => import('./features/informes/informes-list').then((m) => m.InformesList),
  },
  {
    path: 'informes/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/informes/informe-detail').then((m) => m.InformeDetail),
  },
  { path: '**', redirectTo: 'dashboard' },
];
