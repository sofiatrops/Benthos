using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Sampling.Infrastructure.Persistence;

/// <summary>DbContext del módulo de Muestras (esquema <c>sampling</c>), con filtro de tenant y RLS.</summary>
public sealed class SamplingDbContext(
    DbContextOptions<SamplingDbContext> options,
    ITenantContext tenantContext,
    IPublisher? publisher = null)
    : BepDbContextBase(options, tenantContext, publisher)
{
    public const string Schema = "sampling";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SamplingDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
