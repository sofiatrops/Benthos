using Amazon.S3;
using Amazon.S3.Model;
using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bep.Infrastructure.Storage;

/// <summary>
/// Adaptador S3-compatible del puerto <see cref="IObjectStorage"/> (ADR-008).
/// Emite URLs firmadas de vida corta y deriva/valida el prefijo de tenant a partir
/// del <see cref="ITenantContext"/> efectivo, de modo que jamás firma una clave de
/// otro tenant. Funciona contra AWS S3 y MinIO (acceso por ruta).
/// </summary>
internal sealed class S3ObjectStorage(
    IAmazonS3 s3,
    IOptions<ObjectStorageOptions> options,
    ITenantContext tenantContext,
    ILogger<S3ObjectStorage> logger) : IObjectStorage
{
    private readonly ObjectStorageOptions _options = options.Value;

    // El protocolo de las URLs firmadas se toma del esquema del endpoint (MinIO dev = HTTP).
    private Protocol ProtocoloFirma =>
        _options.ServiceUrl?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true
            ? Protocol.HTTP
            : Protocol.HTTPS;

    public async Task<Result<UploadTicket>> CrearTicketSubidaAsync(
        SolicitudSubida solicitud, CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is not { } tenantId)
        {
            return Result.Failure<UploadTicket>(SinTenant());
        }

        if (solicitud.TamanoBytes <= 0 || solicitud.TamanoBytes > _options.TamanoMaximoBytes)
        {
            return Result.Failure<UploadTicket>(Error.Validation(
                "storage.tamano_excedido",
                $"El archivo excede el tamaño máximo permitido ({_options.TamanoMaximoBytes} bytes)."));
        }

        if (_options.ContentTypesPermitidos.Count > 0 &&
            !_options.ContentTypesPermitidos.Contains(solicitud.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<UploadTicket>(Error.Validation(
                "storage.content_type_no_permitido",
                $"El tipo de contenido '{solicitud.ContentType}' no está permitido."));
        }

        var objectKey = ObjectKeys.Construir(tenantId, solicitud.Categoria, solicitud.NombreArchivo);
        var expira = DateTimeOffset.UtcNow.Add(_options.UrlFirmadaVigencia);

        // La URL firmada queda atada al Content-Type declarado: el cliente debe enviar
        // exactamente esa cabecera en el PUT o la firma será inválida (validación real).
        var url = await s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Protocol = ProtocoloFirma,
            Expires = expira.UtcDateTime,
            ContentType = solicitud.ContentType,
        });

        logger.LogInformation(
            "Ticket de subida emitido para tenant {TenantId}, clave {ObjectKey}", tenantId, objectKey);

        return Result.Success(new UploadTicket(objectKey, new Uri(url), solicitud.ContentType, expira));
    }

    public async Task<Result<Uri>> CrearUrlDescargaAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId is not { } tenantId)
        {
            return Result.Failure<Uri>(SinTenant());
        }

        // Defensa frente a IDOR: solo se firman descargas de claves del propio tenant.
        if (!ObjectKeys.PerteneceA(objectKey, tenantId))
        {
            logger.LogWarning(
                "Descarga rechazada: la clave {ObjectKey} no pertenece al tenant {TenantId}", objectKey, tenantId);
            return Result.Failure<Uri>(Error.Forbidden(
                "storage.acceso_denegado", "El objeto solicitado no pertenece a su organización."));
        }

        var url = await s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Protocol = ProtocoloFirma,
            Expires = DateTimeOffset.UtcNow.Add(_options.UrlFirmadaVigencia).UtcDateTime,
        });

        return Result.Success(new Uri(url));
    }

    private static Error SinTenant() => Error.Forbidden(
        "storage.sin_tenant", "No hay un tenant efectivo resuelto para la operación de almacenamiento.");
}
