using Bep.Api.Http;
using Bep.Application.Abstractions.Security;
using Bep.Modules.Insights.Application.Analisis;
using Bep.Modules.Insights.Application.Analisis.GenerarAnalisis;
using Bep.Modules.Insights.Application.Analisis.Queries;
using Bep.Modules.Insights.Domain;
using MediatR;

namespace Bep.Api.Endpoints;

public static class InsightsEndpoints
{
    public static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder app)
    {
        var analisis = app.MapGroup("/api/v1/empresas/{empresaId:guid}/analisis")
            .WithTags("Análisis IA ambiental")
            .RequireAuthorization(BepPolicies.GestionarAnalisis);

        // Genera un borrador a partir de los resultados de laboratorio validados de la campaña.
        analisis.MapPost("/", async (
            Guid empresaId, GenerarAnalisisRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GenerarAnalisisCommand(empresaId, request.CampanaId), ct);
            return result.ToCreatedResult(id => $"/api/v1/empresas/{empresaId}/analisis/{id}");
        })
        .WithName("GenerarAnalisis");

        analisis.MapGet("/", async (
            Guid empresaId, Guid? campanaId, EstadoAnalisis? estado, int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListarAnalisisQuery(empresaId, campanaId, estado, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct);
            return result.ToHttpResult();
        })
        .WithName("ListarAnalisis");

        analisis.MapGet("/{analisisId:guid}", async (
            Guid empresaId, Guid analisisId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ObtenerAnalisisQuery(empresaId, analisisId), ct);
            return result.ToHttpResult();
        })
        .WithName("ObtenerAnalisis");

        analisis.MapPost("/{analisisId:guid}/validar", async (
            Guid empresaId, Guid analisisId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ValidarAnalisisCommand(empresaId, analisisId), ct);
            return result.ToHttpResult();
        })
        .WithName("ValidarAnalisis");

        analisis.MapPost("/{analisisId:guid}/descartar", async (
            Guid empresaId, Guid analisisId, DescartarAnalisisRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DescartarAnalisisCommand(empresaId, analisisId, request.Motivo), ct);
            return result.ToHttpResult();
        })
        .WithName("DescartarAnalisis");

        return app;
    }
}

public sealed record GenerarAnalisisRequest(Guid CampanaId);

public sealed record DescartarAnalisisRequest(string Motivo);
