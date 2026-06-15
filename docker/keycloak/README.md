# Realm de Keycloak (`bep`)

`bep-realm.json` se importa automáticamente al arrancar el contenedor
(`start-dev --import-realm`). Define el cliente, los roles RBAC, los *mappers* de
claims que la API espera y dos usuarios de prueba. **Solo para desarrollo.**

## Qué emite el token (claims que valida la API)

| Claim | Origen | Uso en la API |
|-------|--------|---------------|
| `aud` = `bep-api` | audience mapper | `TokenValidationParameters.ValidateAudience` |
| `roles` (array plano) | realm roles mapper | RBAC (`RoleClaimType = "roles"`) |
| `tenant_id` (GUID) | atributo de usuario | tenant del `ClientUser` (RLS) |
| `principal_type` (`benthos`\|`client`) | atributo de usuario | `BenthosStaff` vs `ClientUser` |

## Usuarios de prueba

| Usuario | Contraseña | `principal_type` | Roles | `tenant_id` |
|---------|-----------|------------------|-------|-------------|
| `staff` | `bep` | `benthos` | super-admin, coordinador, revisor, tecnico | — |
| `cliente` | `bep` | `client` | admin-empresa, usuario-cliente | `00000000-0000-0000-0000-0000000000a1` |

## Obtener un access token (Direct Access Grant, solo dev)

```bash
curl -s http://localhost:8080/realms/bep/protocol/openid-connect/token \
  -d grant_type=password -d client_id=bep-api \
  -d username=staff -d password=bep | jq -r .access_token
```

Úsalo como `Authorization: Bearer <token>` contra la API (`http://localhost:8081`).
Verifica el contexto resuelto con `GET /api/v1/me`.

## Enlace `tenant_id` ↔ empresa demo (automático en Development)

El `tenant_id` del usuario `cliente` (`00000000-0000-0000-0000-0000000000a1`) lo
**aprovisiona automáticamente** la API en Development: al arrancar siembra una
empresa demo con ese `Id` fijo, dos centros, campañas activas y un informe
publicado con su PDF en MinIO (`DevDataSeeder`). Así el Portal muestra datos
reales —incluida la descarga firmada— sin pasos manuales. En producción el
`tenant_id` se asigna al aprovisionar la empresa (`Empresa.Provisionar`).
