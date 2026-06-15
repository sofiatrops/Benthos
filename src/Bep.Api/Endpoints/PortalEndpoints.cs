using Bep.Api.Http;
using Bep.Application.Abstractions.Security;
using Bep.Modules.Portal.Application.Dashboard;
using Bep.Modules.Portal.Application.Informes;
using Bep.Modules.Reporting.Domain;
using MediatR;

namespace Bep.Api.Endpoints;

public static class PortalEndpoints
{
    public static IEndpointRouteBuilder MapPortalEndpoints(this IEndpointRouteBuilder app)
    {
        // El Portal NO recibe empresaId en la URL: el tenant se deriva del JWT del
        // usuario cliente (RF-07-010). Imposible acceder a datos de otra empresa.
        var portal = app.MapGroup("/api/v1/portal")
            .WithTags("Portal Cliente")
            .RequireAuthorization(BepPolicies.PortalCliente);

        portal.MapGet("/dashboard", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PortalDashboardQuery(), ct);
            return result.ToHttpResult();
        })
        .WithName("PortalDashboard");

        portal.MapGet("/informes", async (
            TipoEstudio? tipoEstudio, Guid? centroId, DateOnly? desde, DateOnly? hasta,
            int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var query = new PortalListarInformesPublicadosQuery(
                tipoEstudio, centroId, desde, hasta, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        })
        .WithName("PortalListarInformesPublicados");

        portal.MapGet("/informes/{informeId:guid}", async (
            Guid informeId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PortalObtenerInformeQuery(informeId), ct);
            return result.ToHttpResult();
        })
        .WithName("PortalObtenerInforme");

        return app;
    }
}
