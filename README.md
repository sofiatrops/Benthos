# Benthos Environmental Platform (BEP)

Plataforma SaaS multiempresa de gestión ambiental para Benthos. Backend en
**.NET 10 LTS** (Clean Architecture + DDD táctico + CQRS ligero) sobre
**PostgreSQL 16 / PostGIS**, con aislamiento multi-tenant reforzado por
**Row-Level Security**. Frontend del Portal Cliente en **Angular 20** (standalone) con
login OIDC contra Keycloak.

> Diseño y decisiones: ver [docs/arquitectura/](docs/arquitectura/).
> Requisitos: `SRS_Benthos.pdf` (SRS-BENTHOS-PLATFORM-001).

## Estado: Fase 1 (MVP vertical) ✅ — 79 pruebas en verde

Cadena de valor end-to-end, aislada por tenant (RLS) y auditada:
**empresa → campaña → muestra trazable por cadena de custodia → informe versionado
y publicado → portal del cliente**.

Módulos vivos:

| Módulo | Contenido |
|--------|-----------|
| **M1 Organización** | Empresas (tenants) y centros (PostGIS), CQRS + RBAC. |
| **M2 Campañas** | Ciclo de vida con máquina de estados, responsables, calendario. |
| **M3 Muestras** | Código único + QR, GPS, historial de eventos, cadena de custodia, consulta por QR. |
| **M5 Informes** | Versionado de PDF, flujo de revisión/aprobación/publicación, comentarios internos, archivado lógico. |
| **M7 Portal Cliente** | Dashboard, informes publicados, descarga — tenant derivado del JWT (RF-07-010). |
| **M8 Auditoría** | Tabla append-only inmutable (trigger) alimentada por eventos de dominio. |

Fundaciones (Fase 0): monolito modular .NET 10, **RLS** de dos capas (interceptor
`app.current_tenant` + políticas), auth Keycloak + middleware de tenant, Worker
Hangfire, observabilidad (Serilog/OpenTelemetry/health checks) y CI/CD.

**Almacenamiento de objetos** (ADR-008): adaptador S3-compatible (MinIO/S3) con
**URLs firmadas** de vida corta. El binario nunca atraviesa la API — el cliente
hace `PUT`/`GET` directo al almacén. Las claves viven bajo el prefijo del tenant
(`{tenant_id}/...`) y el adaptador **rechaza firmar claves de otro tenant** (IDOR);
los comandos validan la pertenencia de cada `objectKey` a la empresa. Subida con
validación de tipo de contenido y tamaño.

**Fuera de alcance actual** (Fase 2 / transversales): M4 Laboratorios, M6 IA;
escaneo antivirus de archivos subidos (ADR-008, p. ej. ClamAV en el Worker);
notificaciones; gestión de usuarios cliente (Keycloak); KPIs/series temporales
(dependen de M4/M6); frontend Angular.

## Requisitos

- .NET SDK 10
- Docker (para `docker compose` y las pruebas de integración con Testcontainers)

## Arranque rápido (stack completo)

```bash
cp .env.example .env
docker compose up -d --build
```

| Servicio | URL |
|----------|-----|
| API | http://localhost:8081 (OpenAPI en `/openapi/v1.json`) |
| Keycloak | http://localhost:8080 (admin/admin) |
| MinIO (consola) | http://localhost:9001 |
| PostgreSQL | localhost:5432 (db `bep`) |

> El realm `bep` se importa solo al arrancar Keycloak (cliente `bep-api`, roles,
> mappers de claims `roles`/`tenant_id`/`principal_type` y usuarios de prueba).
> Ver [docker/keycloak/README.md](docker/keycloak/README.md) para obtener un token.

## Desarrollo local (sin contenedorizar la API)

```bash
docker compose up -d postgres keycloak minio   # dependencias
dotnet run --project src/Bep.Api                # aplica migraciones en Development
```

## Frontend — Portal Cliente (Angular 20)

SPA standalone en [frontend/](frontend/). Login OIDC (Authorization Code + PKCE)
contra Keycloak; el access token se adjunta a las llamadas a la API. Vistas:
panel, informes publicados y detalle con **descarga por URL firmada** (ADR-008).

```bash
cd frontend
npm install
npm start                     # http://localhost:4200 (llama a la API en :8081 vía CORS)
```

Requiere la API (`:8081`), Keycloak (`:8080`) y MinIO (`:9000`) arriba. Inicie
sesión con un usuario de prueba del realm (ver
[docker/keycloak/README.md](docker/keycloak/README.md)); el portal es exclusivo de
usuarios de empresa cliente (`principal_type=client`).

## Pruebas

```bash
dotnet test Bep.slnx                 # dominio + integración (requiere Docker)
dotnet test tests/Bep.Modules.Organization.Domain.Tests   # solo unidad (sin Docker)
```

## Migraciones de base de datos

```bash
# Generar una nueva migración para el módulo Organization
ORG=src/Modules/Organization/Bep.Modules.Organization.Infrastructure
dotnet ef migrations add <Nombre> -p $ORG -s $ORG -o Persistence/Migrations

# Aplicar (en CI/CD de despliegue; en local lo hace la API en Development)
dotnet ef database update -p $ORG -s $ORG
```

## Estructura

```
src/
  BuildingBlocks/        SharedKernel, Application.Abstractions, Infrastructure.Common, Infrastructure.Storage
  Modules/Organization/  Domain · Application · Infrastructure
  Bep.Api                Host HTTP (API REST)
  Bep.Worker             Host de trabajos en segundo plano (Hangfire)
tests/                   Pruebas de dominio e integración (aislamiento RLS)
docker/                  Dockerfiles e inicialización de PostgreSQL
docs/arquitectura/       Dossier de decisiones (ADR), dominio, seguridad, plan
frontend/                Portal Cliente en Angular 20 (SPA standalone, OIDC)
docker/keycloak/         Realm import (cliente, roles, mappers, usuarios de prueba)
```
