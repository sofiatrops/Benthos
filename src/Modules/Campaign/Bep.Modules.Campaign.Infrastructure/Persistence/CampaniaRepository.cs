using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Campaign.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Campaign.Infrastructure.Persistence;

internal sealed class CampaniaRepository(CampaignDbContext dbContext) : ICampaniaRepository
{
    public Task<Campania?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Set<Campania>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Campania campania, CancellationToken cancellationToken = default)
        => await dbContext.Set<Campania>().AddAsync(campania, cancellationToken);
}
