using Bep.Api.Http;
using Bep.Application.Abstractions.Security;
using Bep.Modules.Organization.Application.Centros.ListarCentros;
using Bep.Modules.Organization.Application.Centros.RegistrarCentro;
using Bep.Modules.Organization.Application.Empresas.DesactivarEmpresa;
using Bep.Modules.Organization.Application.Empresas.ListarEmpresas;
using Bep.Modules.Organization.Application.Empresas.ObtenerEmpresa;
using Bep.Modules.Organization.Application.Empresas.RegistrarEmpresa;
using MediatR;

namespace Bep.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var empresas = app.MapGroup("/api/v1/empresas").WithTags("Empresas");

        empresas.MapPost("/", async (RegistrarEmpresaRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegistrarEmpresaCommand(request.RazonSocial, request.Rut, request.Rubro), ct);
            return result.ToCreatedResult(id => $"/api/v1/empresas/{id}");
        })
        .RequireAuthorization(BepPolicies.GestionarEmpresas)
        .WithName("RegistrarEmpresa");

        empresas.MapGet("/", async (
            string? search, bool? activa, int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListarEmpresasQuery(search, activa, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization(BepPolicies.GestionarEmpresas)
        .WithName("ListarEmpresas");

        empresas.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ObtenerEmpresaQuery(id), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization(BepPolicies.GestionarEmpresas)
        .WithName("ObtenerEmpresa");

        empresas.MapPost("/{id:guid}/desactivar", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DesactivarEmpresaCommand(id), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization(BepPolicies.GestionarEmpresas)
        .WithName("DesactivarEmpresa");

        empresas.MapPost("/{id:guid}/centros", async (
            Guid id, RegistrarCentroRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new RegistrarCentroCommand(
                id, request.Nombre, request.CodigoInterno, request.Latitud, request.Longitud, request.Region);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(centroId => $"/api/v1/empresas/{id}/centros/{centroId}");
        })
        .RequireAuthorization(BepPolicies.GestionarCentros)
        .WithName("RegistrarCentro");

        empresas.MapGet("/{id:guid}/centros", async (
            Guid id, int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListarCentrosQuery(id, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization(BepPolicies.GestionarCentros)
        .WithName("ListarCentros");

        return app;
    }
}

public sealed record RegistrarEmpresaRequest(string RazonSocial, string Rut, string Rubro);

public sealed record RegistrarCentroRequest(
    string Nombre, string CodigoInterno, double Latitud, double Longitud, string Region);
