using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Reporting.Infrastructure.Persistence;

internal sealed class InformeRepository(ReportingDbContext dbContext) : IInformeRepository
{
    // Las colecciones poseídas (versiones, comentarios, anexos) se cargan con el agregado.
    public Task<Informe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Set<Informe>().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task AddAsync(Informe informe, CancellationToken cancellationToken = default)
        => await dbContext.Set<Informe>().AddAsync(informe, cancellationToken);
}
