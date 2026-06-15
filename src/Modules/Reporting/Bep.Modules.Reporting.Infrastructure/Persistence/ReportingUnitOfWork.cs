using Bep.Modules.Reporting.Application.Abstractions;

namespace Bep.Modules.Reporting.Infrastructure.Persistence;

internal sealed class ReportingUnitOfWork(ReportingDbContext dbContext) : IReportingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
