using Bep.Application.Abstractions;
using Bep.Modules.Insights.Application.Analisis;
using Bep.Modules.Insights.Domain;

namespace Bep.Modules.Insights.Application.Abstractions;

/// <summary>Lado de escritura del agregado <see cref="AnalisisAmbiental"/>.</summary>
public interface IAnalisisRepository
{
    public Task<AnalisisAmbiental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task AddAsync(AnalisisAmbiental analisis, CancellationToken cancellationToken = default);
}

/// <summary>Lado de lectura (CQRS) del módulo de IA Ambiental.</summary>
public interface IInsightsReadService
{
    public Task<AnalisisDto?> GetAnalisisAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<PagedResult<AnalisisResumenDto>> ListAnalisisAsync(
        Guid empresaId, Guid? campanaId, EstadoAnalisis? estado, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Último análisis <b>validado</b> de la empresa (visible al cliente, RF-06-007), o null.</summary>
    public Task<AnalisisDto?> GetUltimoValidadoAsync(Guid empresaId, CancellationToken cancellationToken = default);
}

/// <summary>Unit of Work del módulo de IA Ambiental (evita ambigüedad de DI entre módulos).</summary>
public interface IInsightsUnitOfWork : IUnitOfWork;
