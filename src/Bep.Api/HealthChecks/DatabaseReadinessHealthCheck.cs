using Bep.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bep.Api.HealthChecks;

/// <summary>
/// Comprobación de preparación (readiness) de la base de datos para el endpoint
/// de salud (RF-10-001). Verifica conectividad real, no solo que el proceso vive.
/// </summary>
public sealed class DatabaseReadinessHealthCheck(OrganizationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Base de datos accesible.")
                : HealthCheckResult.Unhealthy("No se pudo conectar a la base de datos.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al verificar la base de datos.", ex);
        }
    }
}
