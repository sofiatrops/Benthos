using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Bep.Infrastructure.Common.DependencyInjection;

public static class TenancyServiceCollectionExtensions
{
    /// <summary>
    /// Registra el contexto de tenant scoped y el interceptor que activa RLS.
    /// Cada módulo añade su DbContext con <c>options.AddInterceptors(...)</c>
    /// resolviendo <see cref="TenantConnectionInterceptor"/> del proveedor.
    /// </summary>
    public static IServiceCollection AddBepTenancy(this IServiceCollection services)
    {
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());
        services.AddScoped<TenantConnectionInterceptor>();
        return services;
    }
}
