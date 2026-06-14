using Bep.Modules.Sampling.Application.Abstractions;
using Bep.Modules.Sampling.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Sampling.Infrastructure.Persistence;

internal sealed class MuestraRepository(SamplingDbContext dbContext) : IMuestraRepository
{
    // Las colecciones poseídas (eventos, custodia) y primitivas se cargan con el agregado.
    public Task<Muestra?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Set<Muestra>().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task AddAsync(Muestra muestra, CancellationToken cancellationToken = default)
        => await dbContext.Set<Muestra>().AddAsync(muestra, cancellationToken);
}
