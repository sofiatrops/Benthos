using Bep.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bep.Modules.Laboratory.Infrastructure.Persistence;

/// <summary>Fábrica en tiempo de diseño para <c>dotnet ef migrations</c> (sin secretos, RNF-SEG-004).</summary>
public sealed class LaboratoryDbContextFactory : IDesignTimeDbContextFactory<LaboratoryDbContext>
{
    public LaboratoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BEP_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=bep;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<LaboratoryDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(LaboratoryDbContext).Assembly.FullName))
            .Options;

        return new LaboratoryDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public bool HasTenant => false;
        public void SetTenant(Guid tenantId) { }
    }
}
