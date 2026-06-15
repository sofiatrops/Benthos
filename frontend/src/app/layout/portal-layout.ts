import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

/** Marco del Portal Cliente: cabecera de navegación + área de contenido. */
@Component({
  selector: 'app-portal-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <a class="brand" routerLink="/">
        <span class="logo">🌊</span>
        <span>Benthos · Portal</span>
      </a>
      <nav class="nav">
        <a routerLink="/portal/dashboard" routerLinkActive="active">Panel</a>
        <a routerLink="/portal/informes" routerLinkActive="active">Informes</a>
      </nav>
      <div class="session">
        <span class="user">{{ auth.username() }}</span>
        <button type="button" class="link" (click)="auth.logout()">Salir</button>
      </div>
    </header>
    <main class="content"><router-outlet /></main>
  `,
})
export class PortalLayout {
  protected readonly auth = inject(AuthService);
}
