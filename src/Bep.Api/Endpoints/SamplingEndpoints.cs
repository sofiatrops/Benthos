using Bep.Api.Http;
using Bep.Application.Abstractions.Security;
using Bep.Modules.Sampling.Application.Muestras.AgregarFoto;
using Bep.Modules.Sampling.Application.Muestras.Custodia;
using Bep.Modules.Sampling.Application.Muestras.Queries;
using Bep.Modules.Sampling.Application.Muestras.RegistrarMuestra;
using Bep.Modules.Sampling.Application.Muestras.TransicionarEstado;
using Bep.Modules.Sampling.Domain;
using MediatR;

namespace Bep.Api.Endpoints;

public static class SamplingEndpoints
{
    public static IEndpointRouteBuilder MapSamplingEndpoints(this IEndpointRouteBuilder app)
    {
        // Muestras dentro de una campaña: registro y listado/exportación.
        var deCampana = app.MapGroup("/api/v1/empresas/{empresaId:guid}/campanas/{campanaId:guid}/muestras")
            .WithTags("Muestras")
            .RequireAuthorization(BepPolicies.GestionarMuestras);

        deCampana.MapPost("/", async (
            Guid empresaId, Guid campanaId, RegistrarMuestraRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new RegistrarMuestraCommand(
                empresaId, campanaId, request.CentroId, request.Tipo, request.Parametros ?? [],
                request.Latitud, request.Longitud, request.PrecisionMetros);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(id => $"/api/v1/empresas/{empresaId}/muestras/{id}");
        })
        .WithName("RegistrarMuestra");

        deCampana.MapGet("/", async (
            Guid empresaId, Guid campanaId, int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListarMuestrasQuery(empresaId, campanaId, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct);
            return result.ToHttpResult();
        })
        .WithName("ListarMuestras");

        // Muestras por identidad: detalle, consulta por QR y operaciones de ciclo de vida.
        var muestras = app.MapGroup("/api/v1/empresas/{empresaId:guid}/muestras")
            .WithTags("Muestras")
            .RequireAuthorization(BepPolicies.GestionarMuestras);

        muestras.MapGet("/{muestraId:guid}", async (
            Guid empresaId, Guid muestraId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ObtenerMuestraQuery(empresaId, muestraId), ct);
            return result.ToHttpResult();
        })
        .WithName("ObtenerMuestra");

        muestras.MapGet("/qr/{codigoQr}", async (
            Guid empresaId, string codigoQr, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ConsultarPorQrQuery(empresaId, codigoQr), ct);
            return result.ToHttpResult();
        })
        .WithName("ConsultarMuestraPorQr");

        muestras.MapPost("/{muestraId:guid}/fotos", async (
            Guid empresaId, Guid muestraId, AgregarFotoRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AgregarFotoCommand(empresaId, muestraId, request.ObjectKey), ct);
            return result.ToHttpResult();
        })
        .WithName("AgregarFotoMuestra");

        muestras.MapPost("/{muestraId:guid}/custodia/transferir", async (
            Guid empresaId, Guid muestraId, TransferirCustodiaRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new TransferirCustodiaCommand(empresaId, muestraId, request.ParaSubjectId), ct);
            return result.ToHttpResult();
        })
        .WithName("TransferirCustodia");

        muestras.MapPost("/{muestraId:guid}/custodia/aceptar", async (
            Guid empresaId, Guid muestraId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AceptarCustodiaCommand(empresaId, muestraId), ct);
            return result.ToHttpResult();
        })
        .WithName("AceptarCustodia");

        muestras.MapPost("/{muestraId:guid}/transicionar", async (
            Guid empresaId, Guid muestraId, TransicionarMuestraRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new TransicionarEstadoMuestraCommand(empresaId, muestraId, request.NuevoEstado, request.Descripcion), ct);
            return result.ToHttpResult();
        })
        .WithName("TransicionarEstadoMuestra");

        return app;
    }
}

public sealed record RegistrarMuestraRequest(
    Guid CentroId,
    TipoMuestra Tipo,
    IReadOnlyList<string> Parametros,
    double Latitud,
    double Longitud,
    double? PrecisionMetros);

public sealed record AgregarFotoRequest(string ObjectKey);

public sealed record TransferirCustodiaRequest(string ParaSubjectId);

public sealed record TransicionarMuestraRequest(EstadoMuestra NuevoEstado, string? Descripcion);
