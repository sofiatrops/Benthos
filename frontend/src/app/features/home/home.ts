import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

/**
 * Página de inicio pública del sitio (landing). Presenta la plataforma y es la
 * puerta de entrada: según la sesión, dirige al portal del cliente o al back-office
 * del personal de Benthos.
 */
@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isAuthenticated = this.auth.isAuthenticated;
  protected readonly isClient = this.auth.isClient;
  protected readonly username = this.auth.username;

  protected acceder(): void {
    if (!this.auth.isAuthenticated()) {
      this.auth.login();
      return;
    }
    this.irAMiArea();
  }

  protected irAMiArea(): void {
    void this.router.navigate([this.auth.isClient() ? '/portal' : '/admin']);
  }

  protected logout(): void {
    this.auth.logout();
  }
}
