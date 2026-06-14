-- Inicialización de PostgreSQL para desarrollo (se ejecuta una sola vez al crear
-- el volumen). Crea el rol de aplicación NO superusuario que hace cumplir la RLS
-- (ADR-004) y habilita las extensiones del dominio.

-- Rol de aplicación: sin superusuario y sin BYPASSRLS, para que las políticas
-- RLS apliquen siempre. La contraseña de desarrollo se sobrescribe vía entorno.
CREATE ROLE bep_app LOGIN PASSWORD 'bep_app_dev' NOSUPERUSER NOBYPASSRLS NOCREATEROLE NOCREATEDB;

-- La base 'bep' la crea el contenedor (POSTGRES_DB). El rol de aplicación la
-- posee para poder aplicar migraciones; con FORCE RLS las políticas aplican
-- incluso al propietario.
ALTER DATABASE bep OWNER TO bep_app;

\connect bep

CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

GRANT ALL ON SCHEMA public TO bep_app;
