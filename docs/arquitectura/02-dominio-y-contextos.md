# 02 — Dominio, Bounded Contexts y Estructura

## 1. Estilo y capas (Clean Architecture)

Monolito modular en una solución .NET. Cada bounded context es un módulo con sus
cuatro capas; las dependencias **siempre apuntan al Dominio** (SRS §2.7.1).

```
Presentación (API REST, .NET 9)
  └─> Aplicación (Casos de uso: Commands/Queries vía MediatR, DTOs, Validadores,
                  interfaces/puertos de repositorio y servicios)
        └─> Dominio (Entidades, Agregados, Objetos de Valor, Eventos, invariantes)
        ^
  Infraestructura (EF Core + PostgreSQL, repositorios, Unit of Work, clientes
                   S3/MinIO, adaptadores de Laboratorio e IA, Serilog, Hangfire)
   ──(implementa puertos declarados en Aplicación: Inversión de Dependencias)──┘
```

## 2. Mapa de Bounded Contexts

Cada contexto ≈ un módulo de negocio. La columna *Módulo SRS* lo vincula al
alcance original.

| Contexto | Módulo SRS | Responsabilidad | Fase |
|----------|-----------|-----------------|------|
| **Identity & Access** | M1 (parcial) + M9 | Usuarios Benthos y de cliente, tenants, roles, ámbitos, sesiones (vía Keycloak). | 1 |
| **Organization** | M1 | Empresa (tenant), Centro, geolocalización (PostGIS). | 1 |
| **Campaign** | M2 | Campaña, ciclo de vida, asignación de responsables. | 1 |
| **Sampling & Custody** | M3 | Muestra, cadena de custodia, QR, GPS, trazabilidad. **Núcleo.** | 1 |
| **Reporting** | M5 | Informe, versionado, flujo de revisión/aprobación, publicación. | 1 |
| **Client Portal** | M7 | Lado de lectura: dashboards, comparativas, descargas (read models). | 1 |
| **Audit** | M8 | Registro inmutable de eventos (transversal, dirigido por eventos). | 1 |
| **Observability** | M10 | Health checks, logging estructurado, métricas, incidencias. | 1 |
| **Laboratory** | M4 | Integración con laboratorios, importación/validación de resultados. | 2 |
| **Environmental AI** | M6 | Extracción, KPIs, resúmenes (asíncrono, anticorrupción). | 2 |

### Contextos transversales (Shared Kernel / Cross-cutting)
- **Security & Tenancy:** resolución de tenant, RLS, autorización rol×ámbito.
- **Audit & Observability:** consumen eventos de dominio sin acoplar el flujo
  transaccional (Observer, SRS §2.7.4).

## 3. Agregados raíz y objetos de valor (SRS §2.7.3)

| Agregado raíz | Invariantes clave | Objetos de Valor |
|---------------|-------------------|------------------|
| **Empresa** (tenant) | Estado activo/inactivo; no eliminación física con historial. | `Rut/IdFiscal`, `DatosContacto` |
| **Centro** | Pertenece a una Empresa; coordenadas válidas. | `CoordenadasGPS`, `Region/Comuna` |
| **Campaña** | Transiciones de estado controladas por rol; no edición en Cerrada/Cancelada salvo excepción auditable. | `RangoFechas`, `TipoCampaña` |
| **Muestra** | Identificador único; cadena de custodia consistente (cada transferencia con usuario+fecha+aceptación). | `CodigoQR`, `CoordenadasGPS`, `ParametroAmbiental` |
| **Informe** | Versionado incremental; visibilidad "Publicado" para cliente; no eliminación física. | `Version`, `MetadatosInforme`, `EstadoRevision` |
| **Usuario** | No autoasignarse rol de mayor privilegio (RF-01-010). | `Email`, `Rol`, `Ambito` |

**Eventos de dominio:** `MuestraRegistrada`, `ResultadoLaboratorioValidado`,
`InformePublicado`, `CampanaCerrada` — consumidos por Audit (M8), notificaciones
y, en Fase 2, IA (M6). **Servicios de dominio:** `ServicioCadenaCustodia`.

## 4. Modelo de identidad: Benthos vs. Tenant (resuelve el riesgo R1)

> Es la decisión de diseño más sensible. La autorización **no** es solo
> "RBAC + tenant_id": es **Rol × Ámbito × Tenant**.

### Dos tipos de sujeto

- **BenthosUser** — personal de Benthos. Opera **transversalmente** sobre datos
  de clientes. Roles: Super Administrador, Coordinador de Operaciones, Técnico
  de Terreno, Revisor/Validador Técnico.
- **ClientUser** — pertenece a **exactamente un** tenant. Roles: Administrador de
  Empresa Cliente, Usuario Cliente (Visualizador).

### Ámbito (Scope)

El ámbito acota *sobre qué datos* puede actuar un rol:

| Sujeto / Rol | Ámbito |
|--------------|--------|
| Super Administrador (Benthos) | Todos los tenants (cross-tenant). |
| Coordinador (Benthos) | Tenants/campañas asignados. |
| Técnico de Terreno (Benthos) | **Centros/campañas asignados** (no todo el tenant). |
| Revisor (Benthos) | Informes/resultados en revisión asignados. |
| Admin Empresa Cliente | Su propio tenant completo. |
| Usuario Cliente | Su tenant, opcionalmente **centros específicos** (RF-07-007). |

### Regla de ejecución por petición

Cada request resuelve un **contexto efectivo de tenant**:

1. Se valida el JWT (Keycloak) → `sub`, tipo de sujeto, roles.
2. Para un **ClientUser**, el tenant efectivo = su tenant (inmutable).
3. Para un **BenthosUser** cross-tenant, el tenant efectivo se fija
   **explícitamente** (p. ej. la campaña/centro objeto de la operación
   determina el tenant), autorizado por su rol cross-tenant. Nunca un bypass
   global silencioso.
4. El tenant efectivo se inyecta en la sesión PostgreSQL
   (`SET app.current_tenant`) → RLS aplica (ADR-004).
5. Para Técnico/Revisor, además se valida el **ámbito** (centro/campaña asignado)
   en la capa de aplicación.

Esto garantiza RF-01-004/005/010, RF-07-001/010 y RNF-SEG-007/008 de forma
estructural, no por convención.

## 5. Estructura de solución propuesta (.NET)

```
Bep.sln
 ├─ src/
 │   ├─ Bep.Api                      # Presentación: controllers, middlewares, OpenAPI
 │   ├─ Bep.Worker                   # Hangfire worker (proceso separado, ADR-005)
 │   ├─ BuildingBlocks/
 │   │   ├─ Bep.SharedKernel         # VOs comunes, base de agregados, eventos
 │   │   ├─ Bep.Application.Abstractions
 │   │   └─ Bep.Infrastructure.Common# EF Core base, RLS, S3, Serilog, OTel
 │   └─ Modules/
 │       ├─ Identity/                # Domain | Application | Infrastructure
 │       ├─ Organization/
 │       ├─ Campaign/
 │       ├─ Sampling/
 │       ├─ Reporting/
 │       ├─ ClientPortal/
 │       ├─ Audit/
 │       ├─ Laboratory/   (Fase 2)
 │       └─ EnvironmentalAi/ (Fase 2)
 └─ tests/
     ├─ <Module>.Domain.Tests        # ≥70% cobertura dominio/aplicación (RNF-MANT-001)
     ├─ <Module>.Application.Tests
     └─ Bep.IntegrationTests         # incluye pruebas de aislamiento cross-tenant
```

Las fronteras entre módulos se verifican en CI con pruebas de arquitectura
(p. ej. un módulo no referencia el `Infrastructure` de otro). El frontend Angular
se versiona y despliega de forma independiente (SRS §2.5).
