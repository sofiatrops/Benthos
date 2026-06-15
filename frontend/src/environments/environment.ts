/**
 * Configuración de entorno del Portal. En desarrollo apunta a la API y al
 * Keycloak locales del docker-compose. Para producción se sustituye por
 * inyección de configuración en el despliegue.
 */
export const environment = {
  production: false,
  apiUrl: 'http://localhost:8081',
  keycloak: {
    issuer: 'http://localhost:8080/realms/bep',
    clientId: 'bep-api',
    scope: 'openid profile email',
  },
};
