using Bep.Modules.Campaign.Domain;

namespace Bep.Modules.Campaign.Application.Abstractions;

/// <summary>Lado de escritura del agregado <see cref="Campania"/>.</summary>
public interface ICampaniaRepository
{
    public Task<Campania?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task AddAsync(Campania campania, CancellationToken cancellationToken = default);
}
