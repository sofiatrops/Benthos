using Bep.Modules.Organization.Application.Abstractions;

namespace Bep.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// Unit of Work del módulo: confirma los cambios del <see cref="OrganizationDbContext"/>,
/// lo que además dispara el despacho de eventos de dominio (ver BepDbContextBase).
/// </summary>
internal sealed class OrganizationUnitOfWork(OrganizationDbContext dbContext) : IOrganizationUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
