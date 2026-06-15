using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Insights.Application;
using Bep.Modules.Insights.Application.Abstractions;
using Bep.Modules.Insights.Application.Generation;
using Bep.Modules.Insights.Infrastructure.Generation;
using Bep.Modules.Insights.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bep.Modules.Insights.Infrastructure;

public static class InsightsModuleExtensions
{
    /// <summary>
    /// Registra el módulo de IA Ambiental (M6): DbContext con RLS, casos de uso (CQRS),
    /// persistencia y el generador de análisis. El proveedor se elige por configuración
    /// (<c>Insights:Provider</c>): <c>deterministic</c> por defecto, o <c>claude</c> si
    /// hay clave (ADR-006). Requiere <c>AddBepTenancy()</c> previo.
    /// </summary>
    public static IServiceCollection AddInsightsModule(
        this IServiceCollection services, string connectionString, IConfiguration configuration)
    {
        services.AddDbContext<InsightsDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(InsightsDbContext).Assembly.FullName));
            options.AddInterceptors(serviceProvider.GetRequiredService<TenantConnectionInterceptor>());
        });

        services.AddInsightsApplication();

        services.AddScoped<IAnalisisRepository, AnalisisRepository>();
        services.AddScoped<IInsightsReadService, InsightsReadService>();
        services.AddScoped<IInsightsUnitOfWork, InsightsUnitOfWork>();

        services.AddOptions<InsightsOptions>().Bind(configuration.GetSection(InsightsOptions.SectionName));

        var options = configuration.GetSection(InsightsOptions.SectionName).Get<InsightsOptions>() ?? new InsightsOptions();
        if (string.Equals(options.Provider, "claude", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(options.ApiKey))
        {
            services.AddHttpClient<IGeneradorAnalisis, ClaudeInsightGenerator>(
                client => client.Timeout = TimeSpan.FromSeconds(60));
        }
        else
        {
            services.AddSingleton<IGeneradorAnalisis, DeterministicInsightGenerator>();
        }

        return services;
    }
}
