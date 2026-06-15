using Bep.Application.Abstractions;
using Bep.Modules.Campaign.Application.Campanias;
using Bep.Modules.Campaign.Domain;

namespace Bep.Modules.Campaign.Application.Abstractions;

/// <summary>Lado de lectura (CQRS) del módulo de Campañas.</summary>
public interface ICampaignReadService
{
    public Task<CampaniaDto?> GetCampaniaAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<PagedResult<CampaniaDto>> ListCampaniasAsync(
        Guid empresaId,
        CampaniaFilter filter,
        PageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>Cuenta las campañas activas (no cerradas ni canceladas) de una empresa, para el dashboard (RF-07-002).</summary>
    public Task<int> CountActivasAsync(Guid empresaId, CancellationToken cancellationToken = default);
}

/// <summary>Criterios de filtrado del listado/calendario de campañas (RF-02-006).</summary>
public sealed record CampaniaFilter(
    EstadoCampania? Estado = null,
    Guid? CentroId = null,
    string? ResponsableSubjectId = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null);
