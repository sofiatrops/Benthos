using Bep.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Audit.Infrastructure.Persistence;

/// <summary>
/// DbContext del módulo de Auditoría (M8). La tabla es global (no tenant-scoped):
/// solo el personal de Benthos consulta la auditoría transversal.
/// </summary>
public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public const string Schema = "audit";

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
