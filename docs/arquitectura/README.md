# Dossier de Arquitectura — Benthos Environmental Platform (BEP)

> Documentos de diseño que **preceden** a la implementación. Cada decisión es
> trazable a un requisito del SRS (`SRS-BENTHOS-PLATFORM-001`).

Este dossier es el entregable de la fase de **diseño** exigida antes de escribir
código. No re-justifica el stack ya elegido en el SRS (Angular / .NET 9 /
PostgreSQL+PostGIS): lo da por válido y cierra los huecos de diseño que el SRS
deja abiertos y que, sin resolverse, se convertirían en deuda técnica o riesgos
de seguridad.

## Índice

| Doc | Contenido |
|-----|-----------|
| [01-decisiones.md](01-decisiones.md) | Registro de decisiones de arquitectura (ADR) y resolución de riesgos. |
| [02-dominio-y-contextos.md](02-dominio-y-contextos.md) | Bounded contexts, agregados, monolito modular y modelo de identidad Benthos-vs-Tenant. |
| [03-seguridad-multitenancy.md](03-seguridad-multitenancy.md) | Aislamiento multi-tenant (RLS), autorización rol×ámbito, secretos, OWASP, storage. |
| [04-plan-de-entrega.md](04-plan-de-entrega.md) | Roadmap por fases, MVP vertical y definición de "listo". |

## Resumen ejecutivo de decisiones

1. **Despliegue v1:** SaaS central en cloud; arquitectura portable en contenedores para no cerrar on-premise.
2. **Estilo arquitectónico:** Monolito modular (Clean Architecture + DDD táctico), preparado para extraer servicios si el volumen lo justifica.
3. **Identidad:** Keycloak como IdP (OIDC/OAuth2); la plataforma gestiona la **autorización** (rol × ámbito × tenant), no las credenciales.
4. **Aislamiento multi-tenant:** filtrado por `tenant_id` en aplicación **+ Row-Level Security en PostgreSQL** como defensa en profundidad.
5. **Trabajos en segundo plano:** worker durable (Hangfire sobre PostgreSQL) en proceso separado; la API permanece *stateless*.
6. **IA (M6):** diferida a Fase 2; gobierno de datos resuelto (LLM comercial con DPA + redacción) antes de construirla.
7. **Alcance v1 (MVP vertical):** M1, M2, M3, M5, M7 + transversales M8/M9/M10. M4 y M6 en Fase 2.
