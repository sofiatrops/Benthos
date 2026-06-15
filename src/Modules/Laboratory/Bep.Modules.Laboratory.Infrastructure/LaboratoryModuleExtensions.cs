using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Laboratory.Application;
using Bep.Modules.Laboratory.Application.Abstractions;
using Bep.Modules.Laboratory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bep.Modules.Laboratory.Infrastructure;

public static class LaboratoryModuleExtensions
{
    /// <summary>
    /// Registra el módulo de Laboratorios (M4): DbContext con RLS, casos de uso (CQRS)
    /// y adaptadores de persistencia. Requiere <c>AddBepTenancy()</c> previo.
    /// </summary>
    public static IServiceCollection AddLaboratoryModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<LaboratoryDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(LaboratoryDbContext).Assembly.FullName));
            options.AddInterceptors(serviceProvider.GetRequiredService<TenantConnectionInterceptor>());
        });

        services.AddLaboratoryApplication();

        services.AddScoped<ILoteResultadosRepository, LoteResultadosRepository>();
        services.AddScoped<ILaboratoryReadService, LaboratoryReadService>();
        services.AddScoped<ILaboratoryUnitOfWork, LaboratoryUnitOfWork>();

        return services;
    }
}
