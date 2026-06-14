using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Sampling.Application;
using Bep.Modules.Sampling.Application.Abstractions;
using Bep.Modules.Sampling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bep.Modules.Sampling.Infrastructure;

public static class SamplingModuleExtensions
{
    /// <summary>
    /// Registra el módulo de Muestras (M3): DbContext con RLS, casos de uso (CQRS)
    /// y adaptadores de persistencia. Requiere <c>AddBepTenancy()</c> previo.
    /// </summary>
    public static IServiceCollection AddSamplingModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SamplingDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(SamplingDbContext).Assembly.FullName));
            options.AddInterceptors(serviceProvider.GetRequiredService<TenantConnectionInterceptor>());
        });

        services.AddSamplingApplication();

        services.AddScoped<IMuestraRepository, MuestraRepository>();
        services.AddScoped<ISamplingReadService, SamplingReadService>();
        services.AddScoped<ISamplingUnitOfWork, SamplingUnitOfWork>();

        return services;
    }
}
