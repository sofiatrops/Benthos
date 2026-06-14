using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// DbContext del módulo Organización. Cada módulo tiene su propio DbContext sobre
/// su esquema (<c>organization</c>), reforzando las fronteras del monolito modular.
/// </summary>
public sealed class OrganizationDbContext(
    DbContextOptions<OrganizationDbContext> options,
    ITenantContext tenantContext,
    IPublisher? publisher = null)
    : BepDbContextBase(options, tenantContext, publisher)
{
    public const string Schema = "organization";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);

        // Filtro de tenant en capa de aplicación para entidades ITenantScoped
        // (primera capa de aislamiento; la segunda es RLS en la base de datos).
        ApplyTenantQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}
