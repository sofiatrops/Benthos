# Benthos Environmental Platform (BEP)

Plataforma SaaS multiempresa de gestión ambiental para Benthos. Backend en
**.NET 10 LTS** (Clean Architecture + DDD táctico + CQRS ligero) sobre
**PostgreSQL 16 / PostGIS**, con aislamiento multi-tenant reforzado por
**Row-Level Security**. Frontend Angular (pendiente de Fase 1).

> Diseño y decisiones: ver [docs/arquitectura/](docs/arquitectura/).
> Requisitos: `SRS_Benthos.pdf` (SRS-BENTHOS-PLATFORM-001).

## Estado: Fase 0 — Fundaciones ✅

Esqueleto productivo del monolito modular:

- Solución .NET con building blocks + módulo **Organization** (Empresa/Centro).
- **Aislamiento multi-tenant** en dos capas: filtro de aplicación (EF Core) +
  **RLS en PostgreSQL**, con interceptor que fija `app.current_tenant` por
  conexión. Verificado por pruebas de integración (deny-by-default y `WITH CHECK`).
- **Autenticación** delegada a Keycloak (JWT); middleware de resolución de tenant.
- **Worker** Hangfire para trabajos en segundo plano (API stateless).
- **Observabilidad**: Serilog + OpenTelemetry + health checks (`/health/live`, `/health/ready`).
- **CI/CD**: build, `dotnet format`, pruebas con cobertura y escaneo de vulnerabilidades.

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

> Keycloak requiere crear el realm `bep` y un cliente `bep-api` la primera vez
> (pendiente de automatizar con import de realm en Fase 1).

## Desarrollo local (sin contenedorizar la API)

```bash
docker compose up -d postgres keycloak minio   # dependencias
dotnet run --project src/Bep.Api                # aplica migraciones en Development
```

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
  BuildingBlocks/        SharedKernel, Application.Abstractions, Infrastructure.Common
  Modules/Organization/  Domain · Application · Infrastructure
  Bep.Api                Host HTTP (API REST)
  Bep.Worker             Host de trabajos en segundo plano (Hangfire)
tests/                   Pruebas de dominio e integración (aislamiento RLS)
docker/                  Dockerfiles e inicialización de PostgreSQL
docs/arquitectura/       Dossier de decisiones (ADR), dominio, seguridad, plan
```
