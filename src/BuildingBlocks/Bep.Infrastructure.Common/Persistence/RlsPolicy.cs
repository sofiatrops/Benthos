namespace Bep.Infrastructure.Common.Persistence;

/// <summary>
/// Genera el SQL de Row-Level Security para una tabla tenant-scoped (ADR-004).
/// Se invoca desde las migraciones de cada módulo (Up/Down), manteniendo la
/// política consistente en toda la plataforma.
///
/// <para>
/// La política deniega por defecto: si <c>app.current_tenant</c> no está fijada o
/// está vacía, <c>NULLIF(...)</c> produce NULL y no se devuelven filas. El rol de
/// aplicación NO debe ser superusuario ni tener BYPASSRLS; las tablas usan
/// FORCE para que la política aplique incluso al propietario.
/// </para>
/// </summary>
public static class RlsPolicy
{
    public const string SessionVariable = "app.current_tenant";

    public static string Enable(string schemaQualifiedTable, string tenantColumn = "tenant_id")
        => $"""
            ALTER TABLE {schemaQualifiedTable} ENABLE ROW LEVEL SECURITY;
            ALTER TABLE {schemaQualifiedTable} FORCE ROW LEVEL SECURITY;
            CREATE POLICY tenant_isolation ON {schemaQualifiedTable}
                USING ({tenantColumn} = NULLIF(current_setting('{SessionVariable}', true), '')::uuid)
                WITH CHECK ({tenantColumn} = NULLIF(current_setting('{SessionVariable}', true), '')::uuid);
            """;

    public static string Disable(string schemaQualifiedTable)
        => $"""
            DROP POLICY IF EXISTS tenant_isolation ON {schemaQualifiedTable};
            ALTER TABLE {schemaQualifiedTable} NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE {schemaQualifiedTable} DISABLE ROW LEVEL SECURITY;
            """;
}
