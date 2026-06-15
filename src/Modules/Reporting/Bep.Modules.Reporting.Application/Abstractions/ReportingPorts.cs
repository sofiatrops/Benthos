using Bep.Application.Abstractions;
using Bep.Modules.Reporting.Application.Informes;
using Bep.Modules.Reporting.Domain;

namespace Bep.Modules.Reporting.Application.Abstractions;

/// <summary>Lado de escritura del agregado <see cref="Informe"/>.</summary>
public interface IInformeRepository
{
    public Task<Informe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task AddAsync(Informe informe, CancellationToken cancellationToken = default);
}

/// <summary>Lado de lectura (CQRS) del módulo de Informes.</summary>
public interface IReportingReadService
{
    public Task<InformeDto?> GetInformeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Listado para personal de Benthos (todos los estados, RF-05-005).</summary>
    public Task<PagedResult<InformeResumenDto>> ListInformesAsync(
        Guid empresaId, EstadoInforme? estado, Guid? campanaId, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Listado restringido a informes Publicados (visibilidad de cliente, RF-05-005), filtrable (RF-07-003).</summary>
    public Task<PagedResult<InformeResumenDto>> ListPublicadosAsync(
        Guid empresaId, PublicadosFilter filter, PageRequest page, CancellationToken cancellationToken = default);
}

/// <summary>Filtros del listado de informes publicados (RF-07-003).</summary>
public sealed record PublicadosFilter(
    TipoEstudio? TipoEstudio = null,
    Guid? CentroId = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null);

/// <summary>Unit of Work del módulo de Informes (evita ambigüedad de DI entre módulos).</summary>
public interface IReportingUnitOfWork : IUnitOfWork;
