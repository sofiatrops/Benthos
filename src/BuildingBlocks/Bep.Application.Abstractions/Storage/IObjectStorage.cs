namespace Bep.Application.Abstractions.Storage;

/// <summary>
/// Puerto de almacenamiento de objetos (PDFs de informes, fotos de muestras,
/// anexos). Sigue el patrón de URLs firmadas de vida corta (ADR-008): el binario
/// nunca atraviesa la API — el cliente sube y descarga directamente contra el
/// almacén S3-compatible usando una URL temporal acotada al tenant.
///
/// <para>
/// El adaptador deriva el prefijo de la clave del <see cref="ITenantContext"/>
/// efectivo y rechaza cualquier clave fuera de ese prefijo, de modo que es
/// imposible firmar una descarga de otro tenant (cierre de IDOR).
/// </para>
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Emite un ticket de subida: genera una clave única bajo el tenant actual y una
    /// URL firmada <c>PUT</c> de vida corta. Falla si el tipo de contenido no está
    /// permitido o el tamaño declarado excede el límite (validación previa a la subida).
    /// </summary>
    public Task<Result<UploadTicket>> CrearTicketSubidaAsync(SolicitudSubida solicitud, CancellationToken cancellationToken);

    /// <summary>
    /// Emite una URL firmada <c>GET</c> de vida corta para descargar un objeto del
    /// tenant actual. Falla con <see cref="ErrorType.Forbidden"/> si la clave no
    /// pertenece al tenant efectivo.
    /// </summary>
    public Task<Result<Uri>> CrearUrlDescargaAsync(string objectKey, CancellationToken cancellationToken);
}

/// <summary>Categorías lógicas de objeto; segundo segmento de la clave tras el tenant.</summary>
public static class CategoriasObjeto
{
    public const string InformesPdf = "informes";
    public const string Anexos = "anexos";
    public const string FotosMuestra = "muestras/fotos";
    public const string Laboratorio = "laboratorio";
}

/// <summary>Datos que el cliente declara para obtener un ticket de subida.</summary>
/// <param name="Categoria">Categoría lógica (ver <see cref="CategoriasObjeto"/>).</param>
/// <param name="NombreArchivo">Nombre original; se sanea y conserva la extensión.</param>
/// <param name="ContentType">Tipo MIME real declarado; se valida contra la lista permitida.</param>
/// <param name="TamanoBytes">Tamaño declarado en bytes; se valida contra el límite.</param>
public sealed record SolicitudSubida(string Categoria, string NombreArchivo, string ContentType, long TamanoBytes);

/// <summary>
/// Ticket de subida. El cliente debe hacer <c>PUT</c> del binario a <see cref="UrlSubida"/>
/// con la cabecera <c>Content-Type</c> declarada, y luego referenciar <see cref="ObjectKey"/>
/// en el comando de dominio correspondiente.
/// </summary>
public sealed record UploadTicket(string ObjectKey, Uri UrlSubida, string ContentType, DateTimeOffset ExpiraUtc);
