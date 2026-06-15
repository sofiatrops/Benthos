using Bep.Api.Http;
using Bep.Application.Abstractions.Security;
using Bep.Modules.Laboratory.Application.LoteResultados;
using Bep.Modules.Laboratory.Application.LoteResultados.ImportarResultados;
using Bep.Modules.Laboratory.Application.LoteResultados.Queries;
using Bep.Modules.Laboratory.Domain;
using MediatR;

namespace Bep.Api.Endpoints;

public static class LaboratoryEndpoints
{
    public static IEndpointRouteBuilder MapLaboratoryEndpoints(this IEndpointRouteBuilder app)
    {
        var lotes = app.MapGroup("/api/v1/empresas/{empresaId:guid}/lotes-resultados")
            .WithTags("Resultados de laboratorio")
            .RequireAuthorization(BepPolicies.GestionarResultados);

        // Ingesta: el archivo (CSV) ya se subió por URL firmada (ADR-008); aquí se
        // referencia por su objectKey y el servidor lo lee, parsea y persiste.
        lotes.MapPost("/", async (
            Guid empresaId, ImportarResultadosRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new ImportarResultadosCommand(
                empresaId, request.CampanaId, request.Laboratorio, request.ObjectKey, request.Formato ?? "csv");
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(r => $"/api/v1/empresas/{empresaId}/lotes-resultados/{r.LoteId}");
        })
        .WithName("ImportarResultados");

        lotes.MapGet("/", async (
            Guid empresaId, Guid? campanaId, EstadoLote? estado, int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListarLotesQuery(empresaId, campanaId, estado, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct);
            return result.ToHttpResult();
        })
        .WithName("ListarLotesResultados");

        lotes.MapGet("/{loteId:guid}", async (
            Guid empresaId, Guid loteId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ObtenerLoteQuery(empresaId, loteId), ct);
            return result.ToHttpResult();
        })
        .WithName("ObtenerLoteResultados");

        lotes.MapPost("/{loteId:guid}/validar", async (
            Guid empresaId, Guid loteId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ValidarLoteCommand(empresaId, loteId), ct);
            return result.ToHttpResult();
        })
        .WithName("ValidarLoteResultados");

        lotes.MapPost("/{loteId:guid}/rechazar", async (
            Guid empresaId, Guid loteId, RechazarLoteRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RechazarLoteCommand(empresaId, loteId, request.Motivo), ct);
            return result.ToHttpResult();
        })
        .WithName("RechazarLoteResultados");

        return app;
    }
}

public sealed record ImportarResultadosRequest(Guid CampanaId, string Laboratorio, string ObjectKey, string? Formato);

public sealed record RechazarLoteRequest(string Motivo);
