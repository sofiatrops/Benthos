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

## Importante: enlazar el `tenant_id` del cliente con una empresa real

El `tenant_id` del usuario `cliente` es un GUID fijo de marcador. Para un flujo
end-to-end del Portal, ese GUID debe corresponder al `Id` de una empresa
registrada. Como `RegistrarEmpresa` genera el `Id`, tras crear la empresa hay que
actualizar el atributo `tenant_id` del usuario (consola de Keycloak →
Users → cliente → Attributes) con el `Id` devuelto. La siembra automática de una
empresa demo con `Id` fijo queda como mejora futura.
