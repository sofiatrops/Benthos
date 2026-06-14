# 01 — Registro de Decisiones de Arquitectura (ADR)

Formato: cada decisión declara **Contexto → Decisión → Consecuencias**, con
trazabilidad al SRS. Estado de todas: **Aceptada (v1.0)**.

---

## ADR-001 — Modelo de despliegue: SaaS central en cloud, portable

**Contexto.** El SRS §2.1 describe a Benthos operando la plataforma de forma
centralizada (multi-tenant). A la vez, §2.4/§2.8.4 exigen contenedores que
corran tanto en cloud como on-premise (RNF-PORT-001/002).

**Decisión.** La v1.0 se despliega como **un único SaaS central en un proveedor
cloud**. Toda la solución se empaqueta en imágenes Docker y no se acopla a
servicios propietarios sin alternativa estándar (RNF-PORT-002), de modo que un
despliegue on-premise futuro no requiera rediseño.

**Consecuencias.**
- Nos apoyamos en servicios gestionados (PostgreSQL gestionado, gestor de
  secretos del proveedor) para reducir carga de DevOps.
- El almacenamiento usa la API S3 (MinIO en dev/on-premise; S3/Blob/GCS en cloud)
  vía patrón Adapter — sin cambios en la capa de aplicación.
- On-premise queda como capacidad latente, no como objetivo de entrega v1.

---

## ADR-002 — Estilo: Monolito modular (no microservicios en v1)

**Contexto.** El SRS adopta Clean Architecture + DDD **táctico** y descarta
explícitamente bounded contexts con BD separadas en v1 (§2.7.3), reservando colas
de mensajería como evolución (KISS, §2.7.5).

**Decisión.** Construir un **monolito modular**: una solución .NET con módulos
(uno por bounded context) aislados en ensamblados/proyectos separados, con
fronteras explícitas, comunicándose **in-process** mediante MediatR y eventos de
dominio. Una sola base de datos PostgreSQL.

**Consecuencias.**
- Despliegue y operación simples para la escala objetivo (decenas de empresas,
  RNF-ESC-002) con bajo costo operativo.
- Las fronteras de módulo se imponen por dependencias de proyecto y reglas de
  arquitectura verificadas en CI, de modo que extraer un servicio (p. ej. IA)
  más adelante sea un refactor acotado, no un rediseño.
- Evita la complejidad prematura de microservicios (sobreingeniería, KISS/YAGNI).

---

## ADR-003 — Identidad delegada a Keycloak; autorización propia

**Contexto.** El SRS (M9) describe JWT + refresh tokens, lockout, políticas de
contraseña y reseteo. Los clientes corporativos (salmoneras, mineras) suelen
exigir **SSO/SAML** corporativo, no contemplado en v1 pero altamente probable.

**Decisión.** Delegar la **autenticación** a **Keycloak** (OIDC/OAuth2,
open-source, self-hostable): emisión de JWT, refresh tokens, MFA, lockout,
políticas de contraseña, reseteo y futuro SSO/SAML. La plataforma BEP conserva
la **autorización de negocio** (rol × ámbito × tenant) y la pertenencia a tenant.

**Consecuencias.**
- Cumplimos RF-09-001..008 y RNF-SEG-002/003 sin construir primitivas de auth.
- SSO corporativo futuro = configuración de federación en Keycloak, no
  desarrollo a medida.
- Coste: un componente más a operar; se contenedoriza junto al resto.
- La API valida el JWT (firma, expiración, audiencia) y mapea el `sub` a su
  modelo de usuario/ámbito interno. Detalle en `03-seguridad-multitenancy.md`.

> *Alternativa descartada:* ASP.NET Identity in-house — viable, pero traslada
> SSO/MFA a desarrollo propio costoso. Keycloak gana por el perfil B2B.

---

## ADR-004 — Aislamiento multi-tenant con Row-Level Security

**Contexto.** RNF-SEG-008 exige aislamiento por `tenant_id` "verificable por
pruebas automatizadas". El SRS filtra en capa de aplicación + "validación en BD".
El olvido de un `WHERE tenant_id` es la causa #1 de fuga de datos en SaaS B2B.

**Decisión.** Defensa en profundidad de **dos capas**:
1. Filtrado por `tenant_id` en la capa de aplicación (query filters de EF Core).
2. **PostgreSQL Row-Level Security (RLS)** sobre toda tabla con `tenant_id`: la
   política exige `tenant_id = current_setting('app.current_tenant')`, que la
   aplicación fija por transacción según el contexto autenticado.

**Consecuencias.**
- Aunque una consulta omita el filtro de aplicación, la BD bloquea el acceso
  cruzado. Es gratuito, nativo y auditable por un tercero.
- El personal de Benthos con rol cross-tenant opera bajo un mecanismo explícito
  de "actuar sobre tenant X" (ver modelo de identidad). Nunca un bypass global
  silencioso.
- Se añaden pruebas de integración que intentan acceso cross-tenant y deben
  fallar (RNF-SEG-008).

---

## ADR-005 — Trabajos en segundo plano con worker durable

**Contexto.** RNF-ESC-004 exige *background jobs* (importación de laboratorio,
IA, reportes grandes) sin bloquear la API; RNF-ESC-001 exige API **stateless**
multi-instancia. Procesar in-process con MediatR no sobrevive a reinicios ni se
reparte entre réplicas.

**Decisión.** Adoptar un **worker durable desde el inicio**: Hangfire con
almacenamiento en PostgreSQL, ejecutado como **proceso/contenedor separado** que
consume la misma BD. La API encola trabajos y permanece stateless.

**Consecuencias.**
- Los jobs persisten, se reintentan con *backoff* (RNF-FIAB-006) y se reparten.
- La API cumple RNF-ESC-001 sin acoplarse a la ejecución de trabajos.
- Evolución futura a cola dedicada (RabbitMQ) si el volumen de integraciones lo
  justifica, sin cambiar la interfaz de encolado.

---

## ADR-006 — IA Ambiental (M6) diferida a Fase 2 con gobierno de datos

**Contexto.** M6 envía informes de clientes a una API LLM externa. Para mineras y
salmoneras esto puede ser **dato confidencial saliendo del perímetro**, un riesgo
contractual/legal que el SRS trata como detalle técnico. RNF-FIAB-007 ya permite
publicar informes sin análisis de IA.

**Decisión.** **Diferir M6 a Fase 2.** Al construirlo: usar un **LLM comercial
bajo DPA** (sin entrenamiento sobre nuestros datos) con **redacción de datos
sensibles** previa al envío, y registrar la trazabilidad del procesamiento
(RF-06-008). El módulo se integra mediante una **capa anticorrupción** (Adapter)
y de forma **asíncrona** (worker), sin acoplar la lógica de negocio.

**Consecuencias.**
- La v1 entrega valor sin depender de decisiones de gobierno de datos aún
  abiertas.
- Cuando entre, la indisponibilidad de la IA no degrada el resto (RNF-FIAB-007,
  RNF-LIM-004).

---

## ADR-007 — Read models para el Portal Cliente (CQRS read side)

**Contexto.** M7 pide comparativas históricas de series temporales y dashboards
(RF-07-002/005/006) sobre la BD transaccional, con carga < 3 s (RNF-REND-002).

**Decisión.** Separar el lado de **lectura** del Portal mediante **proyecciones /
vistas materializadas** mantenidas a partir de eventos de dominio
(`InformePublicado`, `ResultadoLaboratorioValidado`), en línea con el CQRS ligero
del SRS (§2.7.4).

**Consecuencias.**
- Dashboards rápidos sin penalizar el modelo transaccional.
- El refresco se dispara por eventos, no por consultas costosas en caliente.

---

## ADR-008 — Almacenamiento de objetos: seguridad explícita

**Contexto.** Subida de fotos, PDFs y archivos de laboratorio (M3/M4/M5). El SRS
define cifrado en reposo (RNF-SEG-005) pero no controles de subida.

**Decisión.** Sobre el almacenamiento S3-compatible:
- **Prefijo/clave por tenant** (`{tenant_id}/...`) para aislamiento lógico.
- **URLs firmadas** de vida corta para subida/descarga; nunca exposición directa.
- **Validación de content-type real** (no solo extensión) y límite de tamaño.
- **Escaneo antivirus** (p. ej. ClamAV) antes de marcar el objeto como disponible.
- Cifrado del lado del servidor (SSE) y operación transaccional/compensación
  para no dejar metadata huérfana (RNF-LIM-003).

**Consecuencias.** Cierra un vector de ataque (malware/IDOR) que el SRS no
detallaba; coste menor en complejidad de subida.

---

## ADR-009 — Plataforma .NET 10 LTS (desviación del SRS, que indicaba .NET 9)

**Contexto.** El SRS (§2.8.2) especifica ASP.NET Core 9 / .NET 9. A la fecha de
inicio de construcción (jun-2026), **.NET 9 es STS y quedó fuera de soporte en
may-2026**, mientras que **.NET 10 es LTS con soporte hasta nov-2028**.

**Decisión.** Construir sobre **.NET 10 LTS**. El framework se centraliza en
`Directory.Build.props` (`TargetFramework=net10.0`), de modo que un cambio futuro
sea un único punto de edición.

**Consecuencias.**
- Se evita arrancar sobre un runtime sin soporte de seguridad — incoherente con
  el énfasis en seguridad del propio SRS (§3.3.2).
- API, EF Core, Npgsql y herramientas se alinean a la línea 10.x.
- Desviación documentada y trazable; sin impacto en el resto de decisiones
  (Clean Architecture, DDD, CQRS y el stack se mantienen).

> Nota de licencias: **MediatR ≥ 14 pasó a licencia comercial**; se fija la
> última versión libre (12.5.0, Apache 2.0) en `Directory.Packages.props` y se
> marca como punto de vigilancia.
