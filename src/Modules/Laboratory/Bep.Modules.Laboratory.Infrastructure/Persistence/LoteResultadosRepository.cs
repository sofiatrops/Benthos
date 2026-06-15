using Bep.Modules.Laboratory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Laboratory.Infrastructure.Persistence;

internal sealed class LoteResultadosRepository(LaboratoryDbContext dbContext) : ILoteResultadosRepository
{
    public Task<Domain.LoteResultados?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Set<Domain.LoteResultados>().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task AddAsync(Domain.LoteResultados lote, CancellationToken cancellationToken = default)
        => await dbContext.Set<Domain.LoteResultados>().AddAsync(lote, cancellationToken);
}
