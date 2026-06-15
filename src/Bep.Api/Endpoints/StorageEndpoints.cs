using Bep.Api.Http;
using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Security;
using Bep.Application.Abstractions.Storage;

namespace Bep.Api.Endpoints;

/// <summary>
/// Tickets de subida de archivos (ADR-008). El binario nunca atraviesa la API: el
/// cliente obtiene una URL firmada de vida corta y hace <c>PUT</c> directo al
/// almacén, luego referencia la <c>objectKey</c> devuelta en el comando de dominio.
/// </summary>
public static class StorageEndpoints
{
    public static IEndpointRouteBuilder MapStorageEndpoints(this IEndpointRouteBuilder app)
    {
        var uploads = app.MapGroup("/api/v1/empresas/{empresaId:guid}/uploads")
            .WithTags("Almacenamiento")
            .RequireAuthorization(BepPolicies.SubirArchivos);

        uploads.MapPost("/", async (
            Guid empresaId,
            CrearTicketSubidaRequest request,
            ITenantContext tenantContext,
            IObjectStorage storage,
            CancellationToken ct) =>
        {
            // Personal de Benthos opera sobre el tenant indicado de forma explícita:
            // fija el tenant efectivo para que la clave quede bajo su prefijo (RLS/ADR-008).
            tenantContext.SetTenant(empresaId);

            var solicitud = new SolicitudSubida(
                request.Categoria, request.NombreArchivo, request.ContentType, request.TamanoBytes);
            var result = await storage.CrearTicketSubidaAsync(solicitud, ct);
            return result.ToHttpResult();
        })
        .WithName("CrearTicketSubida");

        var downloads = app.MapGroup("/api/v1/empresas/{empresaId:guid}/downloads")
            .WithTags("Almacenamiento")
            .RequireAuthorization(BepPolicies.SubirArchivos);

        // Firma de descarga para personal de Benthos: convierte una object key conocida
        // (PDF de versión, anexo, foto) en una URL temporal. El adaptador rechaza claves
        // fuera del prefijo del tenant (cierre de IDOR).
        downloads.MapPost("/", async (
            Guid empresaId,
            CrearUrlDescargaRequest request,
            ITenantContext tenantContext,
            IObjectStorage storage,
            CancellationToken ct) =>
        {
            tenantContext.SetTenant(empresaId);
            var result = await storage.CrearUrlDescargaAsync(request.ObjectKey, ct);
            return result.IsSuccess
                ? Results.Ok(new { url = result.Value })
                : result.ToHttpResult();
        })
        .WithName("CrearUrlDescarga");

        return app;
    }
}

/// <param name="Categoria">Categoría lógica del objeto (ver <see cref="CategoriasObjeto"/>).</param>
public sealed record CrearTicketSubidaRequest(
    string Categoria, string NombreArchivo, string ContentType, long TamanoBytes);

public sealed record CrearUrlDescargaRequest(string ObjectKey);
