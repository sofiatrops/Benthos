using Bep.Application.Abstractions;
using Bep.Modules.Sampling.Application.Muestras;
using Bep.Modules.Sampling.Domain;

namespace Bep.Modules.Sampling.Application.Abstractions;

/// <summary>Lado de escritura del agregado <see cref="Muestra"/>.</summary>
public interface IMuestraRepository
{
    public Task<Muestra?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task AddAsync(Muestra muestra, CancellationToken cancellationToken = default);
}

/// <summary>Lado de lectura (CQRS) del módulo de Muestras.</summary>
public interface ISamplingReadService
{
    public Task<MuestraDto?> GetMuestraAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Consulta una muestra por su código QR (RF-03-008).</summary>
    public Task<MuestraDto?> GetMuestraPorQrAsync(string codigoQr, CancellationToken cancellationToken = default);

    /// <summary>Lista las muestras de una campaña con su estado de trazabilidad (RF-03-012).</summary>
    public Task<PagedResult<MuestraResumenDto>> ListMuestrasAsync(
        Guid campanaId, PageRequest page, CancellationToken cancellationToken = default);
}

/// <summary>Unit of Work del módulo de Muestras (evita ambigüedad de DI entre módulos).</summary>
public interface ISamplingUnitOfWork : IUnitOfWork;
