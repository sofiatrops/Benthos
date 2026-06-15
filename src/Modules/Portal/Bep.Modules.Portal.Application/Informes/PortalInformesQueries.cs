using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Application.Abstractions.Storage;
using Bep.Modules.Portal.Application.Common;
using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Application.Informes;
using Bep.Modules.Reporting.Domain;

namespace Bep.Modules.Portal.Application.Informes;

/// <summary>Lista los informes publicados de la empresa del cliente, filtrable (RF-07-003).</summary>
public sealed record PortalListarInformesPublicadosQuery(
    TipoEstudio? TipoEstudio = null,
    Guid? CentroId = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<InformeResumenDto>>;

/// <summary>Detalle de un informe publicado para descarga (RF-07-004). Sin comentarios internos (RF-05-004).</summary>
public sealed record PortalObtenerInformeQuery(Guid InformeId) : IQuery<InformePublicadoDetalleDto>;

/// <summary>Vista del informe para el cliente: metadatos, descarga de la versión vigente y anexos.</summary>
public sealed record InformePublicadoDetalleDto(
    Guid Id,
    string Titulo,
    string TipoEstudio,
    DateOnly PeriodoDesde,
    DateOnly PeriodoHasta,
    Guid? CampanaId,
    Guid? CentroId,
    DateTimeOffset? FechaAprobacionUtc,
    int VersionVigenteNumero,
    Uri? UrlDescarga,
    IReadOnlyList<PortalAnexoDto> Anexos);

/// <summary>Anexo visible al cliente con su URL de descarga firmada (sin exponer la clave interna).</summary>
public sealed record PortalAnexoDto(string Descripcion, DateTimeOffset FechaUtc, Uri UrlDescarga);

internal sealed class PortalListarInformesPublicadosHandler(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IReportingReadService reportingReadService)
    : IQueryHandler<PortalListarInformesPublicadosQuery, PagedResult<InformeResumenDto>>
{
    public async Task<Result<PagedResult<InformeResumenDto>>> Handle(
        PortalListarInformesPublicadosQuery query, CancellationToken cancellationToken)
    {
        var tenant = PortalTenant.Resolver(currentUser, tenantContext);
        if (tenant.IsFailure)
        {
            return Result.Failure<PagedResult<InformeResumenDto>>(tenant.Error!);
        }

        var filter = new PublicadosFilter(query.TipoEstudio, query.CentroId, query.Desde, query.Hasta);
        var page = new PageRequest(query.Page, query.PageSize);
        var result = await reportingReadService.ListPublicadosAsync(tenant.Value, filter, page, cancellationToken);
        return Result.Success(result);
    }
}

internal sealed class PortalObtenerInformeHandler(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IReportingReadService reportingReadService,
    IObjectStorage objectStorage)
    : IQueryHandler<PortalObtenerInformeQuery, InformePublicadoDetalleDto>
{
    public async Task<Result<InformePublicadoDetalleDto>> Handle(
        PortalObtenerInformeQuery query, CancellationToken cancellationToken)
    {
        var tenant = PortalTenant.Resolver(currentUser, tenantContext);
        if (tenant.IsFailure)
        {
            return Result.Failure<InformePublicadoDetalleDto>(tenant.Error!);
        }

        var informe = await reportingReadService.GetInformeAsync(query.InformeId, cancellationToken);

        // RLS garantiza que solo se obtiene un informe del propio tenant; además,
        // el cliente solo puede ver informes Publicados (RF-05-005).
        if (informe is null || informe.Estado != nameof(EstadoInforme.Publicado))
        {
            return Result.Failure<InformePublicadoDetalleDto>(Error.NotFound(
                "portal.informe.no_disponible", "El informe no existe o no está publicado."));
        }

        var vigente = informe.Versiones.FirstOrDefault(v => v.Numero == informe.VersionVigenteNumero);

        // URLs firmadas de vida corta (ADR-008): el cliente descarga directo del
        // almacén; nunca se expone la clave interna del objeto.
        Uri? urlDescarga = null;
        if (vigente is not null)
        {
            var firma = await objectStorage.CrearUrlDescargaAsync(vigente.ObjectKey, cancellationToken);
            if (firma.IsFailure)
            {
                return Result.Failure<InformePublicadoDetalleDto>(firma.Error!);
            }

            urlDescarga = firma.Value;
        }

        var anexos = new List<PortalAnexoDto>(informe.Anexos.Count);
        foreach (var anexo in informe.Anexos)
        {
            var firma = await objectStorage.CrearUrlDescargaAsync(anexo.ObjectKey, cancellationToken);
            if (firma.IsFailure)
            {
                return Result.Failure<InformePublicadoDetalleDto>(firma.Error!);
            }

            anexos.Add(new PortalAnexoDto(anexo.Descripcion, anexo.FechaUtc, firma.Value));
        }

        var detalle = new InformePublicadoDetalleDto(
            informe.Id,
            informe.Titulo,
            informe.TipoEstudio,
            informe.PeriodoDesde,
            informe.PeriodoHasta,
            informe.CampanaId,
            informe.CentroId,
            informe.FechaAprobacionUtc,
            informe.VersionVigenteNumero,
            urlDescarga,
            anexos);

        return Result.Success(detalle);
    }
}
