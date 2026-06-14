using Bep.Modules.Organization.Domain;

namespace Bep.Modules.Organization.Application.Abstractions;

/// <summary>Lado de escritura del agregado <see cref="Empresa"/> (Repository Pattern).</summary>
public interface IEmpresaRepository
{
    /// <summary>Carga la empresa con sus centros, o null si no existe.</summary>
    public Task<Empresa?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<bool> ExistsByRutAsync(string rut, CancellationToken cancellationToken = default);

    public Task AddAsync(Empresa empresa, CancellationToken cancellationToken = default);
}
