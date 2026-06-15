import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

/** Marco del back-office de personal de Benthos. */
@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar admin">
      <a class="brand" routerLink="/">
        <span class="logo">🌊</span>
        <span>Benthos · Operaciones</span>
      </a>
      <nav class="nav">
        <a routerLink="/admin/empresas" routerLinkActive="active">Empresas</a>
      </nav>
      <div class="session">
        <span class="user">{{ auth.username() }}</span>
        @for (rol of auth.roles(); track rol) {
          <span class="rol">{{ rol }}</span>
        }
        <button type="button" class="link" (click)="auth.logout()">Salir</button>
      </div>
    </header>
    <main class="content"><router-outlet /></main>
  `,
  styles: [`
    .admin { border-bottom-color: #0f766e; }
    .rol {
      font-size: 0.72rem;
      background: #ecfdf5;
      color: #047857;
      border-radius: 999px;
      padding: 0.1rem 0.5rem;
    }
  `],
})
export class AdminLayout {
  protected readonly auth = inject(AuthService);
}
