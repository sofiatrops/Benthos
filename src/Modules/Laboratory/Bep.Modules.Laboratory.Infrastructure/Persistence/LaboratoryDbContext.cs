using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Laboratory.Infrastructure.Persistence;

/// <summary>DbContext del módulo de Laboratorios (esquema <c>laboratory</c>), con filtro de tenant y RLS.</summary>
public sealed class LaboratoryDbContext(
    DbContextOptions<LaboratoryDbContext> options,
    ITenantContext tenantContext,
    IPublisher? publisher = null)
    : BepDbContextBase(options, tenantContext, publisher)
{
    public const string Schema = "laboratory";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LaboratoryDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
