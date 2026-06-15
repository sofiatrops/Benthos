using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Reporting.Application;
using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bep.Modules.Reporting.Infrastructure;

public static class ReportingModuleExtensions
{
    /// <summary>
    /// Registra el módulo de Informes (M5): DbContext con RLS, casos de uso (CQRS)
    /// y adaptadores de persistencia. Requiere <c>AddBepTenancy()</c> previo.
    /// </summary>
    public static IServiceCollection AddReportingModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ReportingDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ReportingDbContext).Assembly.FullName));
            options.AddInterceptors(serviceProvider.GetRequiredService<TenantConnectionInterceptor>());
        });

        services.AddReportingApplication();

        services.AddScoped<IInformeRepository, InformeRepository>();
        services.AddScoped<IReportingReadService, ReportingReadService>();
        services.AddScoped<IReportingUnitOfWork, ReportingUnitOfWork>();

        return services;
    }
}
