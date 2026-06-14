using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bep.Modules.Audit.Infrastructure.Persistence;

/// <summary>Fábrica en tiempo de diseño para <c>dotnet ef migrations</c> (sin secretos, RNF-SEG-004).</summary>
public sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BEP_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=bep;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName))
            .Options;

        return new AuditDbContext(options);
    }
}
