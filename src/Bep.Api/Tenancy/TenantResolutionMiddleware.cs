using Bep.Application.Abstractions;

namespace Bep.Api.Tenancy;

/// <summary>
/// Resuelve el tenant efectivo de la petición y lo fija en <see cref="ITenantContext"/>,
/// que a su vez activa la RLS en PostgreSQL (ADR-004).
///
/// <list type="bullet">
///   <item><b>Usuario cliente:</b> el tenant es su propia empresa (claim del JWT), inmutable.</item>
///   <item><b>Personal de Benthos:</b> el tenant objetivo se fija explícitamente por
///   operación (no aquí). Mientras no se fije, las tablas tenant-scoped no
///   devuelven filas (denegación por defecto).</item>
/// </list>
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        if (currentUser is { PrincipalType: PrincipalType.ClientUser, TenantId: { } tenantId })
        {
            tenantContext.SetTenant(tenantId);
        }

        await next(context);
    }
}

public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseBepTenantResolution(this IApplicationBuilder app)
        => app.UseMiddleware<TenantResolutionMiddleware>();
}
