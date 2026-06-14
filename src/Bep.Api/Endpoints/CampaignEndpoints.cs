using Bep.Api.Http;
using Bep.Application.Abstractions.Security;
using Bep.Modules.Campaign.Application.Campanias.AsignarResponsables;
using Bep.Modules.Campaign.Application.Campanias.CrearCampana;
using Bep.Modules.Campaign.Application.Campanias.ListarCampanas;
using Bep.Modules.Campaign.Application.Campanias.ObtenerCampana;
using Bep.Modules.Campaign.Application.Campanias.TransicionarEstado;
using Bep.Modules.Campaign.Domain;
using MediatR;

namespace Bep.Api.Endpoints;

public static class CampaignEndpoints
{
    public static IEndpointRouteBuilder MapCampaignEndpoints(this IEndpointRouteBuilder app)
    {
        var campanas = app.MapGroup("/api/v1/empresas/{empresaId:guid}/campanas")
            .WithTags("Campañas")
            .RequireAuthorization(BepPolicies.GestionarCampanas);

        campanas.MapPost("/", async (
            Guid empresaId, CrearCampanaRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CrearCampanaCommand(
                empresaId, request.Nombre, request.Descripcion, request.Tipo,
                request.FechaInicio, request.FechaFin, request.CentroIds);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(id => $"/api/v1/empresas/{empresaId}/campanas/{id}");
        })
        .WithName("CrearCampana");

        campanas.MapGet("/", async (
            Guid empresaId,
            EstadoCampania? estado, Guid? centroId, string? responsable,
            DateOnly? desde, DateOnly? hasta, int page, int pageSize,
            ISender sender, CancellationToken ct) =>
        {
            var query = new ListarCampanasQuery(
                empresaId, estado, centroId, responsable, desde, hasta,
                page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        })
        .WithName("ListarCampanas");

        campanas.MapGet("/{campanaId:guid}", async (
            Guid empresaId, Guid campanaId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ObtenerCampanaQuery(empresaId, campanaId), ct);
            return result.ToHttpResult();
        })
        .WithName("ObtenerCampana");

        campanas.MapPost("/{campanaId:guid}/responsables", async (
            Guid empresaId, Guid campanaId, AsignarResponsablesRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AsignarResponsablesCommand(empresaId, campanaId, request.Responsables);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .WithName("AsignarResponsables");

        campanas.MapPost("/{campanaId:guid}/transicionar", async (
            Guid empresaId, Guid campanaId, TransicionarEstadoRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new TransicionarEstadoCommand(empresaId, campanaId, request.NuevoEstado);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .WithName("TransicionarEstadoCampana");

        return app;
    }
}

public sealed record CrearCampanaRequest(
    string Nombre,
    string Descripcion,
    TipoCampania Tipo,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    IReadOnlyList<Guid> CentroIds);

public sealed record AsignarResponsablesRequest(IReadOnlyList<ResponsableInput> Responsables);

public sealed record TransicionarEstadoRequest(EstadoCampania NuevoEstado);
