using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Insights.Application.Abstractions;
using Bep.Modules.Laboratory.Application.Abstractions;
using Bep.Modules.Portal.Application.Common;
using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Application.Informes;

namespace Bep.Modules.Portal.Application.Dashboard;

/// <summary>Resumen del Portal Cliente: campañas activas, últimos informes publicados y KPIs (RF-07-002).</summary>
public sealed record PortalDashboardQuery : IQuery<DashboardDto>;

public sealed record DashboardDto(
    int CampanasActivas,
    IReadOnlyList<InformeResumenDto> UltimosInformesPublicados,
    IReadOnlyList<KpiDto> Kpis,
    string? ResumenAnalisis);

/// <summary>Indicador clave ambiental. Se alimenta de M4 (Laboratorios); más adelante también de M6 (IA).</summary>
public sealed record KpiDto(string Nombre, double Valor, string Unidad);

internal sealed class PortalDashboardHandler(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    ICampaignReadService campaignReadService,
    IReportingReadService reportingReadService,
    ILaboratoryReadService laboratoryReadService,
    IInsightsReadService insightsReadService)
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

        // KPIs ambientales derivados de los lotes de laboratorio validados (RF-07-005).
        var kpisLab = await laboratoryReadService.GetKpisAsync(empresaId, cancellationToken);
        var kpis = kpisLab.Select(k => new KpiDto(k.Nombre, k.Valor, k.Unidad)).ToList();

        // Solo se muestra el análisis de IA si un profesional lo validó (RF-06-007).
        var analisis = await insightsReadService.GetUltimoValidadoAsync(empresaId, cancellationToken);

        return Result.Success(new DashboardDto(campanasActivas, publicados.Items, kpis, analisis?.Resumen));
    }
}
