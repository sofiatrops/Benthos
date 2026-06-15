using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Laboratory.Application.Abstractions;
using Bep.Modules.Laboratory.Domain;

namespace Bep.Modules.Laboratory.Application.LoteResultados.Queries;

/// <summary>Detalle de un lote de resultados con sus mediciones.</summary>
public sealed record ObtenerLoteQuery(Guid EmpresaId, Guid LoteId) : IQuery<LoteResultadosDto>;

/// <summary>Lista los lotes de resultados de la empresa, filtrable por campaña y estado.</summary>
public sealed record ListarLotesQuery(
    Guid EmpresaId, Guid? CampanaId = null, EstadoLote? Estado = null, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<LoteResumenDto>>;

internal sealed class ObtenerLoteHandler(
    ITenantContext tenantContext, ILaboratoryReadService readService)
    : IQueryHandler<ObtenerLoteQuery, LoteResultadosDto>
{
    public async Task<Result<LoteResultadosDto>> Handle(ObtenerLoteQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var lote = await readService.GetLoteAsync(query.LoteId, cancellationToken);
        return lote is null
            ? Result.Failure<LoteResultadosDto>(Error.NotFound("laboratory.lote.no_encontrado", $"No existe el lote {query.LoteId}."))
            : Result.Success(lote);
    }
}

internal sealed class ListarLotesHandler(
    ITenantContext tenantContext, ILaboratoryReadService readService)
    : IQueryHandler<ListarLotesQuery, PagedResult<LoteResumenDto>>
{
    public async Task<Result<PagedResult<LoteResumenDto>>> Handle(
        ListarLotesQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var page = new PageRequest(query.Page, query.PageSize);
        var result = await readService.ListLotesAsync(query.EmpresaId, query.CampanaId, query.Estado, page, cancellationToken);
        return Result.Success(result);
    }
}
