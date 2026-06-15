using Bep.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bep.Modules.Reporting.Infrastructure.Persistence;

/// <summary>Fábrica en tiempo de diseño para <c>dotnet ef migrations</c> (sin secretos, RNF-SEG-004).</summary>
public sealed class ReportingDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BEP_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=bep;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ReportingDbContext).Assembly.FullName))
            .Options;

        return new ReportingDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public bool HasTenant => false;
        public void SetTenant(Guid tenantId) { }
    }
}
