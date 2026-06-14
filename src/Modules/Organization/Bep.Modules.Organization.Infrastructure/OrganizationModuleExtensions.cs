using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Organization.Application;
using Bep.Modules.Organization.Application.Abstractions;
using Bep.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bep.Modules.Organization.Infrastructure;

public static class OrganizationModuleExtensions
{
    /// <summary>
    /// Registra el módulo Organización completo: DbContext sobre PostgreSQL con
    /// el interceptor de tenant (RLS), casos de uso (CQRS) y adaptadores de
    /// persistencia. Requiere <c>AddBepTenancy()</c> previo.
    /// </summary>
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OrganizationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(OrganizationDbContext).Assembly.FullName));
            options.AddInterceptors(serviceProvider.GetRequiredService<TenantConnectionInterceptor>());
        });

        services.AddOrganizationApplication();

        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IOrganizationReadService, OrganizationReadService>();
        services.AddScoped<IOrganizationUnitOfWork, OrganizationUnitOfWork>();

        return services;
    }
}
