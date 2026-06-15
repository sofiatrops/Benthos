import { Injectable, computed, inject, signal } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { authCodeFlowConfig } from './auth.config';

/**
 * Sesión del usuario derivada del access token de Keycloak. Los claims relevantes
 * (`roles`, `tenant_id`, `principal_type`) viven en el access token, así que se
 * decodifica directamente en lugar de usar solo el id_token.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauth = inject(OAuthService);
  private readonly _claims = signal<Record<string, unknown> | null>(null);

  readonly claims = this._claims.asReadonly();
  readonly isAuthenticated = computed(() => this._claims() !== null);
  readonly username = computed(() => (this._claims()?.['preferred_username'] as string) ?? '');
  readonly principalType = computed(() => (this._claims()?.['principal_type'] as string) ?? null);
  readonly tenantId = computed(() => (this._claims()?.['tenant_id'] as string) ?? null);
  readonly isClient = computed(() => this.principalType() === 'client');

  readonly roles = computed<string[]>(() => {
    const raw = this._claims()?.['roles'];
    if (Array.isArray(raw)) return raw as string[];
    return typeof raw === 'string' ? [raw] : [];
  });

  /** Configura OIDC, procesa el retorno del login y publica los claims. */
  async init(): Promise<void> {
    this.oauth.configure(authCodeFlowConfig);
    try {
      await this.oauth.loadDiscoveryDocumentAndTryLogin();
      if (this.oauth.hasValidAccessToken()) {
        this._claims.set(this.decode(this.oauth.getAccessToken()));
      }
    } catch (error) {
      console.error('No se pudo inicializar la sesión OIDC', error);
    }
  }

  login(): void {
    this.oauth.initCodeFlow();
  }

  logout(): void {
    this._claims.set(null);
    this.oauth.logOut();
  }

  private decode(token: string): Record<string, unknown> | null {
    try {
      const payload = token.split('.')[1];
      const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(json) as Record<string, unknown>;
    } catch {
      return null;
    }
  }
}
