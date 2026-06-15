using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Portal.Application.Common;
using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Application.Informes;

namespace Bep.Modules.Portal.Application.Dashboard;

/// <summary>Resumen del Portal Cliente: campañas activas, últimos informes publicados y KPIs (RF-07-002).</summary>
public sealed record PortalDashboardQuery : IQuery<DashboardDto>;

public sealed record DashboardDto(
    int CampanasActivas,
    IReadOnlyList<InformeResumenDto> UltimosInformesPublicados,
    IReadOnlyList<KpiDto> Kpis);

/// <summary>Indicador clave ambiental. Se poblará a partir de M4 (Laboratorios) y M6 (IA).</summary>
public sealed record KpiDto(string Nombre, double Valor, string Unidad);

internal sealed class PortalDashboardHandler(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    ICampaignReadService campaignReadService,
    IReportingReadService reportingReadService)
    : IQueryHandler<PortalDashboardQuery, DashboardDto>
{
    private const int UltimosInformes = 5;

    public async Task<Result<DashboardDto>> Handle(PortalDashboardQuery query, CancellationToken cancellationToken)
    {
        var tenant = PortalTenant.Resolver(currentUser, tenantContext);
        if (tenant.IsFailure)
        {
            return Result.Failure<DashboardDto>(tenant.Error!);
        }

        var empresaId = tenant.Value;

        var campanasActivas = await campaignReadService.CountActivasAsync(empresaId, cancellationToken);
        var publicados = await reportingReadService.ListPublicadosAsync(
            empresaId, new PublicadosFilter(), new PageRequest(1, UltimosInformes), cancellationToken);

        // Los KPIs ambientales dependen de datos de parámetros (M4 Laboratorios / M6 IA),
        // aún no disponibles; se entregan vacíos por ahora.
        return Result.Success(new DashboardDto(campanasActivas, publicados.Items, []));
    }
}
