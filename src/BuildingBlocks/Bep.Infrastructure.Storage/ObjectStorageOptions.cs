using System.ComponentModel.DataAnnotations;

namespace Bep.Infrastructure.Storage;

/// <summary>
/// Configuración del almacenamiento de objetos S3-compatible (ADR-008). En
/// desarrollo apunta a MinIO; en producción a S3 gestionado. Los secretos llegan
/// por variables de entorno, nunca versionados (RNF-SEG-004).
/// </summary>
public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    /// <summary>Endpoint del servicio S3 (p. ej. <c>http://minio:9000</c>). Vacío usa AWS por región.</summary>
    public string? ServiceUrl { get; set; }

    [Required]
    public string Bucket { get; set; } = "bep";

    public string Region { get; set; } = "us-east-1";

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>MinIO requiere acceso por ruta (<c>host/bucket/key</c>) en lugar de virtual-host.</summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>Crear el bucket al arrancar si no existe (conveniencia de desarrollo).</summary>
    public bool CrearBucketSiNoExiste { get; set; }

    /// <summary>Vigencia de las URLs firmadas. Corta por diseño (ADR-008).</summary>
    [Range(typeof(TimeSpan), "00:00:30", "01:00:00")]
    public TimeSpan UrlFirmadaVigencia { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Tamaño máximo de subida aceptado (RNF-LIM-003 / control de abuso).</summary>
    [Range(1, long.MaxValue)]
    public long TamanoMaximoBytes { get; set; } = 50L * 1024 * 1024; // 50 MiB

    /// <summary>Tipos MIME permitidos para subida. Vacío = sin restricción (no recomendado).</summary>
    public IList<string> ContentTypesPermitidos { get; set; } = new List<string>
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp",
        "text/csv",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };
}
