using Bep.Modules.Insights.Application.Abstractions;
using Bep.Modules.Insights.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Insights.Infrastructure.Persistence;

internal sealed class AnalisisRepository(InsightsDbContext dbContext) : IAnalisisRepository
{
    public Task<AnalisisAmbiental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Set<AnalisisAmbiental>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(AnalisisAmbiental analisis, CancellationToken cancellationToken = default)
        => await dbContext.Set<AnalisisAmbiental>().AddAsync(analisis, cancellationToken);
}
