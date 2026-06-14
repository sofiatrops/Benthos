using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Campaign.Infrastructure.Persistence;

/// <summary>DbContext del módulo de Campañas (esquema <c>campaign</c>), con filtro de tenant y RLS.</summary>
public sealed class CampaignDbContext(
    DbContextOptions<CampaignDbContext> options,
    ITenantContext tenantContext,
    IPublisher? publisher = null)
    : BepDbContextBase(options, tenantContext, publisher)
{
    public const string Schema = "campaign";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampaignDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
