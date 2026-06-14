# 03 — Seguridad y Multi-tenancy

Seguridad **desde el diseño** (SRS §3.3.2, OWASP ASVS L2). Este documento define
los mecanismos; los IDs entre paréntesis son requisitos del SRS.

## 1. Autenticación (Keycloak — ADR-003)

- Keycloak emite **JWT de acceso de vida corta (~15 min)** y **refresh tokens
  (~7 días)**, revocables individualmente (RNF-SEG-003, RF-09-001/002/003).
- Lockout tras N intentos fallidos, políticas de contraseña, reseteo con enlace
  de un solo uso, MFA: configurados en Keycloak (RF-09-004/005/006).
- Hashing de contraseñas: Argon2/bcrypt gestionado por Keycloak (RNF-SEG-002).
- La API es **Resource Server**: valida firma, expiración y *audience* del JWT
  (RFC 7519), y mapea `sub` → usuario/ámbito interno.
- Revocación de todas las sesiones de un usuario (RF-09-007) → vía admin de
  Keycloak + invalidación de refresh tokens.

## 2. Autorización (Rol × Ámbito × Tenant)

Ver modelo completo en [02-dominio-y-contextos.md](02-dominio-y-contextos.md) §4.

- **RBAC** con los roles del Apéndice C del SRS (RF-01-004); permisos granulares
  por módulo/acción (RF-01-006) evaluados en la capa de aplicación con políticas.
- **Mínimo privilegio** (RNF-SEG-007): cada rol accede solo a lo necesario.
- Un usuario **no** puede autoasignarse ni asignar un rol de mayor privilegio que
  el suyo (RF-01-010) — invariante de dominio, no validación de UI.

## 3. Aislamiento multi-tenant (ADR-004)

**Dos capas, defensa en profundidad** (RNF-SEG-008):

1. **Aplicación:** *global query filter* de EF Core por `tenant_id`, derivado del
   contexto efectivo de tenant resuelto por petición.
2. **Base de datos:** **Row-Level Security** en toda tabla con `tenant_id`.

```sql
-- Patrón por tabla con tenant_id
ALTER TABLE sampling.muestra ENABLE ROW LEVEL SECURITY;
ALTER TABLE sampling.muestra FORCE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON sampling.muestra
  USING (tenant_id = current_setting('app.current_tenant')::uuid);
```

- La aplicación ejecuta `SET LOCAL app.current_tenant = '<uuid>'` al inicio de
  cada transacción, según el contexto autenticado.
- El rol de BD de la aplicación **no** es superusuario y **no** tiene `BYPASSRLS`.
- El acceso cross-tenant de Benthos se modela fijando explícitamente el tenant
  objetivo (nunca desactivando RLS).
- **Pruebas automatizadas obligatorias:** un test que intente leer datos de otro
  tenant debe fallar (RNF-SEG-008).

## 4. Gestión de secretos (RNF-SEG-004)

- **Nunca** secretos en código ni en control de versiones.
- Dev/on-premise: variables de entorno vía *Docker secrets* / `.env` no
  versionado.
- Cloud: gestor de secretos del proveedor (AWS Secrets Manager / Azure Key Vault).
- Cadenas de conexión, claves de firma, credenciales de laboratorio e IA: todas
  como secretos inyectados (SRS §2.8.7).

## 5. Datos en tránsito y en reposo

- **Tránsito:** HTTPS/TLS 1.2+ extremo a extremo; HTTP simple se redirige o
  rechaza (RNF-SEG-001).
- **Reposo:** columnas sensibles cifradas en BD y cifrado del lado del servidor
  (SSE) en el almacenamiento de objetos (RNF-SEG-005). Copias de seguridad
  cifradas en tránsito y reposo (RNF-DATOS-007).

## 6. Endurecimiento de la API (OWASP)

- Validación y *sanitización* de toda entrada; protección contra inyección SQL
  (consultas parametrizadas/EF Core) y XSS (RNF-SEG-006, ASVS L2).
- **Cabeceras de seguridad** estándar: `Content-Security-Policy`,
  `Strict-Transport-Security`, `X-Content-Type-Options`, `X-Frame-Options`
  (RNF-SEG-010).
- **Rate limiting** por usuario/empresa con respuesta `429` ante exceso
  (RNF-LIM-005).
- **Idempotencia** en operaciones críticas (claves de idempotencia) para que un
  reintento de red no duplique registros (RNF-LIM-007).
- **Concurrencia optimista** (token de versión/ETag) en entidades editables
  concurrentemente (RNF-LIM-001).

## 7. Subida de archivos (ADR-008)

Prefijo por tenant + URLs firmadas + validación de content-type real + límite de
tamaño + escaneo antivirus antes de disponibilizar + operación
transaccional/compensación para no dejar metadata huérfana (RNF-LIM-003).

## 8. Auditoría (M8)

- Registro **inmutable** de accesos, descargas, modificaciones (valores
  antes/después), eliminaciones lógicas y cambios de estado (RF-08-001..005).
- Los registros de auditoría **no** pueden editarse ni eliminarse desde la
  aplicación (RF-08-007) — tabla *append-only*, sin permisos UPDATE/DELETE para
  el rol de aplicación.
- Retención mínima configurable, por defecto **5 años** (RF-08-009).
- Alimentada por **eventos de dominio**, desacoplada del flujo transaccional.
