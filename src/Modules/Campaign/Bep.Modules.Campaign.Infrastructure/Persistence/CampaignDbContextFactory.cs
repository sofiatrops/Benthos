using Bep.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bep.Modules.Campaign.Infrastructure.Persistence;

/// <summary>Fábrica en tiempo de diseño para <c>dotnet ef migrations</c> (sin secretos, RNF-SEG-004).</summary>
public sealed class CampaignDbContextFactory : IDesignTimeDbContextFactory<CampaignDbContext>
{
    public CampaignDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BEP_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=bep;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<CampaignDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(CampaignDbContext).Assembly.FullName))
            .Options;

        return new CampaignDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public bool HasTenant => false;
        public void SetTenant(Guid tenantId) { }
    }
}
