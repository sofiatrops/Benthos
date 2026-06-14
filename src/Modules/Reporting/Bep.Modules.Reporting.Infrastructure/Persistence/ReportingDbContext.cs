using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Reporting.Infrastructure.Persistence;

/// <summary>DbContext del módulo de Informes (esquema <c>reporting</c>), con filtro de tenant y RLS.</summary>
public sealed class ReportingDbContext(
    DbContextOptions<ReportingDbContext> options,
    ITenantContext tenantContext,
    IPublisher? publisher = null)
    : BepDbContextBase(options, tenantContext, publisher)
{
    public const string Schema = "reporting";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
