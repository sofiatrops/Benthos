using Bep.Modules.Laboratory.Application.Abstractions;

namespace Bep.Modules.Laboratory.Infrastructure.Persistence;

internal sealed class LaboratoryUnitOfWork(LaboratoryDbContext dbContext) : ILaboratoryUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
