# 04 — Plan de Entrega

Estrategia: **MVP vertical** (rebanada de negocio end-to-end) en lugar de capas
horizontales, para validar valor real temprano sobre el núcleo de trazabilidad.

## Fase 0 — Fundaciones (habilitadores transversales)

Antes de la primera funcionalidad de negocio. Sin esto, todo lo demás acumula
deuda.

- Estructura de solución (monolito modular) + pipeline CI/CD (GitHub Actions:
  build, tests, linters, imagen Docker — RNF-MANT-002/003).
- `docker-compose` de desarrollo: api, worker, postgres+PostGIS, keycloak, minio,
  observabilidad (SRS §2.8.4/§2.8.8).
- Plantilla de **EF Core Migrations** versionadas (RNF-DATOS-006).
- Esqueleto de seguridad: integración Keycloak + middleware de tenant + RLS +
  test de aislamiento que falle correctamente (ADR-003/004).
- Observabilidad base: Serilog + OpenTelemetry + endpoint `/health`
  (RF-10-001/002).
- Auditoría base: pipeline de eventos de dominio → tabla append-only (M8).

## Fase 1 — MVP de negocio (módulos núcleo)

Orden por dependencia. Cada módulo se entrega con API + frontend + pruebas
(≥70% dominio/aplicación, RNF-MANT-001) y sus RF de prioridad **Alta**.

1. **M1 — Organización & Identidad.** Empresas (tenants), centros (PostGIS),
   usuarios, roles, ámbitos. Base de todo lo demás.
2. **M2 — Campañas.** Ciclo de vida, asignaciones, calendario.
3. **M3 — Muestras & Cadena de Custodia.** *El núcleo de valor:* QR, GPS, fotos,
   trazabilidad punta a punta, captura en navegador (PWA responsive).
4. **M5 — Informes.** Carga PDF, versionado, flujo de aprobación, publicación
   controlada.
5. **M7 — Portal Cliente (lectura).** Dashboards y descargas con read models
   (ADR-007); aislamiento estricto verificado (RF-07-010).

Transversales activos durante toda la Fase 1: **M8 (Auditoría)**, **M9
(Seguridad)**, **M10 (Observabilidad)**.

**Hito de Fase 1:** un cliente puede ver, en su portal, un informe publicado cuya
muestra es trazable hasta su registro en terreno. Cierra OB-1, OB-2, OB-4, OB-6,
OB-7 (matriz del SRS, Apéndice B).

## Fase 2 — Integraciones y valor agregado

6. **M4 — Laboratorios.** Carga de archivos estructurados (CSV/Excel) primero;
   adaptadores API por laboratorio después (patrón Strategy/Adapter).
7. **M6 — IA Ambiental.** Asíncrona, con gobierno de datos resuelto (ADR-006):
   extracción de parámetros, KPIs, resúmenes; siempre validados por un
   profesional antes de publicar (RF-06-007/010).

**Hito de Fase 2:** resultados de laboratorio importados y validados alimentan
KPIs y comparativas en el portal. Cierra OB-3, OB-5.

## Definición de "Listo" (por incremento)

Un incremento está listo cuando:
- Cumple sus RF de prioridad Alta y los RNF transversales aplicables.
- Tiene cobertura de pruebas ≥70% en dominio/aplicación (RNF-MANT-001) **y**
  pruebas de aislamiento cross-tenant cuando toca datos de tenant.
- Documentación OpenAPI generada y actualizada (RNF-MANT-002).
- Pasa el pipeline de CI/CD (linters, build, tests, escaneo de dependencias).
- Acciones relevantes emiten eventos de auditoría (M8).

## Fuera de alcance v1 (confirmado del SRS §1.2.3)

App móvil nativa, facturación, IoT/telemetría en tiempo real, optimización de
rutas, i18n completa (arquitectura preparada, no traducciones). Captura
**offline** marcada como riesgo de negocio temprano (zonas de baja
conectividad): el modelo de datos la anticipa (IDs en cliente + idempotencia),
aunque la sincronización offline no se implemente en v1.
