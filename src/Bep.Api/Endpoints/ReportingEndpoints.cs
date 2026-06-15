using Bep.Api.Http;
using Bep.Application.Abstractions.Security;
using Bep.Modules.Reporting.Application.Informes;
using Bep.Modules.Reporting.Application.Informes.CrearInforme;
using Bep.Modules.Reporting.Application.Informes.Queries;
using Bep.Modules.Reporting.Domain;
using MediatR;

namespace Bep.Api.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var informes = app.MapGroup("/api/v1/empresas/{empresaId:guid}/informes")
            .WithTags("Informes")
            .RequireAuthorization(BepPolicies.GestionarInformes);

        informes.MapPost("/", async (
            Guid empresaId, CrearInformeRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CrearInformeCommand(
                empresaId, request.Titulo, request.TipoEstudio, request.PeriodoDesde, request.PeriodoHasta,
                request.CampanaId, request.CentroId, request.PrimeraVersionObjectKey);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(id => $"/api/v1/empresas/{empresaId}/informes/{id}");
        })
        .WithName("CrearInforme");

        informes.MapGet("/", async (
            Guid empresaId, EstadoInforme? estado, Guid? campanaId, int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListarInformesQuery(empresaId, estado, campanaId, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct);
            return result.ToHttpResult();
        })
        .WithName("ListarInformes");

        informes.MapGet("/publicados", async (
            Guid empresaId, int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListarPublicadosQuery(empresaId, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct);
            return result.ToHttpResult();
        })
        .WithName("ListarInformesPublicados");

        informes.MapGet("/{informeId:guid}", async (
            Guid empresaId, Guid informeId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ObtenerInformeQuery(empresaId, informeId), ct);
            return result.ToHttpResult();
        })
        .WithName("ObtenerInforme");

        informes.MapPost("/{informeId:guid}/versiones", async (
            Guid empresaId, Guid informeId, CargarVersionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CargarVersionCommand(empresaId, informeId, request.ObjectKey), ct);
            return result.ToHttpResult();
        })
        .WithName("CargarVersionInforme");

        informes.MapPost("/{informeId:guid}/comentarios", async (
            Guid empresaId, Guid informeId, AgregarComentarioRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AgregarComentarioCommand(empresaId, informeId, request.Texto), ct);
            return result.ToHttpResult();
        })
        .WithName("AgregarComentarioInforme");

        informes.MapPost("/{informeId:guid}/anexos", async (
            Guid empresaId, Guid informeId, AgregarAnexoRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AgregarAnexoCommand(empresaId, informeId, request.ObjectKey, request.Descripcion), ct);
            return result.ToHttpResult();
        })
        .WithName("AgregarAnexoInforme");

        informes.MapPost("/{informeId:guid}/transicionar", async (
            Guid empresaId, Guid informeId, TransicionarInformeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new TransicionarEstadoInformeCommand(empresaId, informeId, request.NuevoEstado), ct);
            return result.ToHttpResult();
        })
        .WithName("TransicionarEstadoInforme");

        informes.MapPost("/{informeId:guid}/archivar", async (
            Guid empresaId, Guid informeId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ArchivarInformeCommand(empresaId, informeId), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization(BepPolicies.ArchivarInformes)
        .WithName("ArchivarInforme");

        return app;
    }
}

public sealed record CrearInformeRequest(
    string Titulo,
    TipoEstudio TipoEstudio,
    DateOnly PeriodoDesde,
    DateOnly PeriodoHasta,
    Guid? CampanaId,
    Guid? CentroId,
    string PrimeraVersionObjectKey);

public sealed record CargarVersionRequest(string ObjectKey);

public sealed record AgregarComentarioRequest(string Texto);

public sealed record AgregarAnexoRequest(string ObjectKey, string Descripcion);

public sealed record TransicionarInformeRequest(EstadoInforme NuevoEstado);
