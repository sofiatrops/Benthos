using System.Data.Common;
using Bep.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bep.Infrastructure.Common.Persistence;

/// <summary>
/// Fija la variable de sesión <c>app.current_tenant</c> en cada conexión que EF
/// Core abre, a partir del tenant efectivo de la petición. Es la pieza que activa
/// la Row-Level Security de PostgreSQL (ADR-004): las políticas RLS comparan
/// <c>tenant_id</c> contra esta variable.
///
/// <para>
/// Si no hay tenant resuelto, la variable se fija a cadena vacía. Las políticas
/// RLS la convierten a NULL y por tanto <b>no devuelven filas</b> de tablas
/// tenant-scoped (denegación por defecto). Npgsql resetea el estado de sesión al
/// devolver la conexión al pool, por lo que el valor no se filtra entre peticiones.
/// </para>
/// </summary>
public sealed class TenantConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    private const string SetTenantSql = "SELECT set_config('app.current_tenant', @tenant, false);";

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ApplyTenantAsync(connection, cancellationToken).ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyTenantAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    private async Task ApplyTenantAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SetTenantSql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenant";
        parameter.Value = tenantContext.TenantId?.ToString() ?? string.Empty;
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
