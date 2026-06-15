using Bep.Modules.Insights.Application.Abstractions;

namespace Bep.Modules.Insights.Infrastructure.Persistence;

internal sealed class InsightsUnitOfWork(InsightsDbContext dbContext) : IInsightsUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
