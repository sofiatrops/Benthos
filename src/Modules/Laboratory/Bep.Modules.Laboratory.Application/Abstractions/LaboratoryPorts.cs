using Bep.Application.Abstractions;
using Bep.Modules.Laboratory.Application.LoteResultados;
using Bep.Modules.Laboratory.Domain;

namespace Bep.Modules.Laboratory.Application.Abstractions;

/// <summary>Lado de escritura del agregado <see cref="LoteResultados"/>.</summary>
public interface ILoteResultadosRepository
{
    public Task<Domain.LoteResultados?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task AddAsync(Domain.LoteResultados lote, CancellationToken cancellationToken = default);
}

/// <summary>Lado de lectura (CQRS) del módulo de Laboratorios.</summary>
public interface ILaboratoryReadService
{
    public Task<LoteResultadosDto?> GetLoteAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<PagedResult<LoteResumenDto>> ListLotesAsync(
        Guid empresaId, Guid? campanaId, EstadoLote? estado, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicadores agregados sobre los lotes <b>validados</b> de la empresa, para el
    /// dashboard del portal (RF-07-005). Mientras no haya resultados, devuelve vacío.
    /// </summary>
    public Task<IReadOnlyList<LaboratorioKpi>> GetKpisAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mediciones de los lotes <b>validados</b> de una campaña. Insumo del análisis de
    /// IA ambiental (M6), que solo interpreta datos ya validados por un profesional.
    /// </summary>
    public Task<IReadOnlyList<ResultadoParametroDto>> GetResultadosValidadosPorCampanaAsync(
        Guid empresaId, Guid campanaId, CancellationToken cancellationToken = default);
}

/// <summary>Unit of Work del módulo de Laboratorios (evita ambigüedad de DI entre módulos).</summary>
public interface ILaboratoryUnitOfWork : IUnitOfWork;
