import { AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';

/**
 * Authorization Code + PKCE contra Keycloak (cliente público `bep-api`).
 * `requireHttps` se desactiva solo para el Keycloak HTTP de desarrollo.
 */
export const authCodeFlowConfig: AuthConfig = {
  issuer: environment.keycloak.issuer,
  redirectUri: window.location.origin + '/',
  postLogoutRedirectUri: window.location.origin + '/',
  clientId: environment.keycloak.clientId,
  responseType: 'code',
  scope: environment.keycloak.scope,
  requireHttps: false,
  showDebugInformation: false,
  strictDiscoveryDocumentValidation: false,
};
