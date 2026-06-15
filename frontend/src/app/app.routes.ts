import { Routes } from '@angular/router';
import { clientGuard, staffGuard } from './core/auth/role.guards';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    path: 'portal',
    canActivate: [clientGuard],
    loadComponent: () => import('./layout/portal-layout').then((m) => m.PortalLayout),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard) },
      { path: 'informes', loadComponent: () => import('./features/informes/informes-list').then((m) => m.InformesList) },
      { path: 'informes/:id', loadComponent: () => import('./features/informes/informe-detail').then((m) => m.InformeDetail) },
    ],
  },
  {
    path: 'admin',
    canActivate: [staffGuard],
    loadComponent: () => import('./layout/admin-layout').then((m) => m.AdminLayout),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'empresas' },
      { path: 'empresas', loadComponent: () => import('./features/admin/empresas-list').then((m) => m.EmpresasList) },
      { path: 'empresas/:id', loadComponent: () => import('./features/admin/empresa-detail').then((m) => m.EmpresaDetail) },
    ],
  },
  { path: '**', redirectTo: '' },
];
