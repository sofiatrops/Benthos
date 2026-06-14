using Bep.Application.Abstractions;
using Bep.Modules.Organization.Application.Centros;
using Bep.Modules.Organization.Application.Empresas;

namespace Bep.Modules.Organization.Application.Abstractions;

/// <summary>
/// Lado de lectura (CQRS): proyecciones optimizadas a DTOs, sin materializar
/// agregados completos. Implementado en infraestructura con EF Core.
/// </summary>
public interface IOrganizationReadService
{
    public Task<EmpresaDto?> GetEmpresaAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<PagedResult<EmpresaDto>> ListEmpresasAsync(
        string? search, bool? activa, PageRequest page, CancellationToken cancellationToken = default);

    public Task<PagedResult<CentroDto>> ListCentrosAsync(
        Guid empresaId, PageRequest page, CancellationToken cancellationToken = default);
}
