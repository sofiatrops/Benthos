using Bep.Modules.Sampling.Application.Abstractions;

namespace Bep.Modules.Sampling.Infrastructure.Persistence;

internal sealed class SamplingUnitOfWork(SamplingDbContext dbContext) : ISamplingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
