using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Domain;

namespace Bep.Modules.Reporting.Application.Informes.Queries;

/// <summary>Detalle completo de un informe para personal de Benthos.</summary>
public sealed record ObtenerInformeQuery(Guid EmpresaId, Guid InformeId) : IQuery<InformeDto>;

/// <summary>Listado de informes (todos los estados) para personal de Benthos.</summary>
public sealed record ListarInformesQuery(
    Guid EmpresaId, EstadoInforme? Estado = null, Guid? CampanaId = null, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<InformeResumenDto>>;

/// <summary>Listado restringido a informes publicados (visibilidad del cliente, RF-05-005).</summary>
public sealed record ListarPublicadosQuery(Guid EmpresaId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<InformeResumenDto>>;

internal sealed class ObtenerInformeHandler(IReportingReadService readService, ITenantContext tenantContext)
    : IQueryHandler<ObtenerInformeQuery, InformeDto>
{
    public async Task<Result<InformeDto>> Handle(ObtenerInformeQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var informe = await readService.GetInformeAsync(query.InformeId, cancellationToken);
        return informe is null
            ? Result.Failure<InformeDto>(Error.NotFound("reporting.informe.no_encontrado", $"No existe el informe {query.InformeId}."))
            : Result.Success(informe);
    }
}

internal sealed class ListarInformesHandler(IReportingReadService readService, ITenantContext tenantContext)
    : IQueryHandler<ListarInformesQuery, PagedResult<InformeResumenDto>>
{
    public async Task<Result<PagedResult<InformeResumenDto>>> Handle(ListarInformesQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var page = new PageRequest(query.Page, query.PageSize);
        var result = await readService.ListInformesAsync(query.EmpresaId, query.Estado, query.CampanaId, page, cancellationToken);
        return Result.Success(result);
    }
}

internal sealed class ListarPublicadosHandler(IReportingReadService readService, ITenantContext tenantContext)
    : IQueryHandler<ListarPublicadosQuery, PagedResult<InformeResumenDto>>
{
    public async Task<Result<PagedResult<InformeResumenDto>>> Handle(ListarPublicadosQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var page = new PageRequest(query.Page, query.PageSize);
        var result = await readService.ListPublicadosAsync(query.EmpresaId, new PublicadosFilter(), page, cancellationToken);
        return Result.Success(result);
    }
}
