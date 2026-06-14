using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Sampling.Application.Abstractions;

namespace Bep.Modules.Sampling.Application.Muestras.Queries;

/// <summary>Obtiene el detalle y la trazabilidad completa de una muestra (RF-03-011).</summary>
public sealed record ObtenerMuestraQuery(Guid EmpresaId, Guid MuestraId) : IQuery<MuestraDto>;

/// <summary>Consulta una muestra por su código QR escaneado (RF-03-008).</summary>
public sealed record ConsultarPorQrQuery(Guid EmpresaId, string CodigoQr) : IQuery<MuestraDto>;

/// <summary>Lista las muestras de una campaña con su estado de trazabilidad (RF-03-012).</summary>
public sealed record ListarMuestrasQuery(Guid EmpresaId, Guid CampanaId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<MuestraResumenDto>>;

internal sealed class ObtenerMuestraHandler(ISamplingReadService readService, ITenantContext tenantContext)
    : IQueryHandler<ObtenerMuestraQuery, MuestraDto>
{
    public async Task<Result<MuestraDto>> Handle(ObtenerMuestraQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var muestra = await readService.GetMuestraAsync(query.MuestraId, cancellationToken);
        return muestra is null
            ? Result.Failure<MuestraDto>(Error.NotFound("sampling.muestra.no_encontrada", $"No existe la muestra {query.MuestraId}."))
            : Result.Success(muestra);
    }
}

internal sealed class ConsultarPorQrHandler(ISamplingReadService readService, ITenantContext tenantContext)
    : IQueryHandler<ConsultarPorQrQuery, MuestraDto>
{
    public async Task<Result<MuestraDto>> Handle(ConsultarPorQrQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var muestra = await readService.GetMuestraPorQrAsync(query.CodigoQr, cancellationToken);
        return muestra is null
            ? Result.Failure<MuestraDto>(Error.NotFound("sampling.muestra.qr_no_encontrado", "No se encontró una muestra para ese código QR."))
            : Result.Success(muestra);
    }
}

internal sealed class ListarMuestrasHandler(ISamplingReadService readService, ITenantContext tenantContext)
    : IQueryHandler<ListarMuestrasQuery, PagedResult<MuestraResumenDto>>
{
    public async Task<Result<PagedResult<MuestraResumenDto>>> Handle(ListarMuestrasQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);
        var page = new PageRequest(query.Page, query.PageSize);
        var result = await readService.ListMuestrasAsync(query.CampanaId, page, cancellationToken);
        return Result.Success(result);
    }
}
