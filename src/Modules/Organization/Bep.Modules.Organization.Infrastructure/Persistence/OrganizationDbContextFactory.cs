using Bep.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bep.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// Fábrica en tiempo de diseño para <c>dotnet ef migrations</c>. Toma la cadena de
/// conexión de la variable de entorno <c>BEP_DB_CONNECTION</c> (sin secretos en
/// el repositorio, RNF-SEG-004) o un valor de desarrollo por defecto.
/// </summary>
public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BEP_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=bep;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(OrganizationDbContext).Assembly.FullName))
            .Options;

        return new OrganizationDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;

        public bool HasTenant => false;

        public void SetTenant(Guid tenantId)
        {
            // Sin efecto en tiempo de diseño.
        }
    }
}
