using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Insights.Application.Abstractions;
using Bep.Modules.Insights.Domain;

namespace Bep.Modules.Insights.Application.Analisis.Queries;

/// <summary>Detalle de un análisis ambiental.</summary>
public sealed record ObtenerAnalisisQuery(Guid EmpresaId, Guid AnalisisId) : IQuery<AnalisisDto>;

/// <summary>Lista los análisis de la empresa, filtrable por campaña y estado.</summary>
public sealed record ListarAnalisisQuery(
    Guid EmpresaId, Guid? CampanaId = null, EstadoAnalisis? Estado = null, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<AnalisisResumenDto>>;

internal sealed class ObtenerAnalisisHandler(
    ITenantContext tenantContext, IInsightsReadService readService)
    : IQueryHandler<ObtenerAnalisisQuery, AnalisisDto>
{
    public async Task<Result<AnalisisDto>> Handle(ObtenerAnalisisQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var analisis = await readService.GetAnalisisAsync(query.AnalisisId, cancellationToken);
        return analisis is null
            ? Result.Failure<AnalisisDto>(Error.NotFound("insights.analisis.no_encontrado", $"No existe el análisis {query.AnalisisId}."))
            : Result.Success(analisis);
    }
}

internal sealed class ListarAnalisisHandler(
    ITenantContext tenantContext, IInsightsReadService readService)
    : IQueryHandler<ListarAnalisisQuery, PagedResult<AnalisisResumenDto>>
{
    public async Task<Result<PagedResult<AnalisisResumenDto>>> Handle(
        ListarAnalisisQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var page = new PageRequest(query.Page, query.PageSize);
        var result = await readService.ListAnalisisAsync(query.EmpresaId, query.CampanaId, query.Estado, page, cancellationToken);
        return Result.Success(result);
    }
}
