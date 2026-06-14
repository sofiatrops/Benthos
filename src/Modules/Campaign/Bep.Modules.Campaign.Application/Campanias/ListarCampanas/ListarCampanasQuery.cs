using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Campaign.Domain;

namespace Bep.Modules.Campaign.Application.Campanias.ListarCampanas;

/// <summary>Lista/calendario de campañas de una empresa, filtrable (RF-02-006).</summary>
public sealed record ListarCampanasQuery(
    Guid EmpresaId,
    EstadoCampania? Estado = null,
    Guid? CentroId = null,
    string? ResponsableSubjectId = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<CampaniaDto>>;

internal sealed class ListarCampanasHandler(
    ICampaignReadService readService,
    ITenantContext tenantContext)
    : IQueryHandler<ListarCampanasQuery, PagedResult<CampaniaDto>>
{
    public async Task<Result<PagedResult<CampaniaDto>>> Handle(ListarCampanasQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);

        var filter = new CampaniaFilter(query.Estado, query.CentroId, query.ResponsableSubjectId, query.Desde, query.Hasta);
        var page = new PageRequest(query.Page, query.PageSize);
        var result = await readService.ListCampaniasAsync(query.EmpresaId, filter, page, cancellationToken);

        return Result.Success(result);
    }
}
