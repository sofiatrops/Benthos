using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Campaign.Application;
using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Campaign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bep.Modules.Campaign.Infrastructure;

public static class CampaignModuleExtensions
{
    /// <summary>
    /// Registra el módulo de Campañas: DbContext con RLS, casos de uso (CQRS) y
    /// adaptadores de persistencia. Requiere <c>AddBepTenancy()</c> previo.
    /// </summary>
    public static IServiceCollection AddCampaignModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CampaignDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(CampaignDbContext).Assembly.FullName));
            options.AddInterceptors(serviceProvider.GetRequiredService<TenantConnectionInterceptor>());
        });

        services.AddCampaignApplication();

        services.AddScoped<ICampaniaRepository, CampaniaRepository>();
        services.AddScoped<ICampaignReadService, CampaignReadService>();
        services.AddScoped<ICampaignUnitOfWork, CampaignUnitOfWork>();

        return services;
    }
}
