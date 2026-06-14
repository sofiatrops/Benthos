using Bep.Modules.Campaign.Application.Abstractions;

namespace Bep.Modules.Campaign.Infrastructure.Persistence;

internal sealed class CampaignUnitOfWork(CampaignDbContext dbContext) : ICampaignUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
