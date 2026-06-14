using Bep.Modules.Organization.Application.Abstractions;
using Bep.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Organization.Infrastructure.Persistence;

internal sealed class EmpresaRepository(OrganizationDbContext dbContext) : IEmpresaRepository
{
    public Task<Empresa?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Set<Empresa>()
            .Include(e => e.Centros)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExistsByRutAsync(string rut, CancellationToken cancellationToken = default)
    {
        var rutValueObject = Rut.Create(rut);
        return dbContext.Set<Empresa>().AnyAsync(e => e.Rut == rutValueObject, cancellationToken);
    }

    public async Task AddAsync(Empresa empresa, CancellationToken cancellationToken = default)
        => await dbContext.Set<Empresa>().AddAsync(empresa, cancellationToken);
}
