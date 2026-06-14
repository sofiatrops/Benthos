using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Organization.Application.Abstractions;

namespace Bep.Modules.Organization.Application.Centros.ListarCentros;

/// <summary>Lista los centros de una empresa con paginación (RF-01-008).</summary>
public sealed record ListarCentrosQuery(Guid EmpresaId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<CentroDto>>;

internal sealed class ListarCentrosHandler(
    IOrganizationReadService readService,
    ITenantContext tenantContext)
    : IQueryHandler<ListarCentrosQuery, PagedResult<CentroDto>>
{
    public async Task<Result<PagedResult<CentroDto>>> Handle(ListarCentrosQuery query, CancellationToken cancellationToken)
    {
        // Habilita la RLS sobre 'centro' para el tenant consultado (ADR-004).
        tenantContext.SetTenant(query.EmpresaId);

        var page = new PageRequest(query.Page, query.PageSize);
        var result = await readService.ListCentrosAsync(query.EmpresaId, page, cancellationToken);
        return Result.Success(result);
    }
}
