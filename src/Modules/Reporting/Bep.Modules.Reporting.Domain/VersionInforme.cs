using Bep.SharedKernel;

namespace Bep.Modules.Reporting.Domain;

/// <summary>
/// Versión de un informe (RF-05-002). Cada carga genera una nueva versión y las
/// anteriores se conservan; nunca se modifican.
/// </summary>
public sealed class VersionInforme : Entity<Guid>
{
    internal VersionInforme(int numero, string objectKey, DateTimeOffset fechaCargaUtc, string? cargadoPorSubjectId)
        : base(Guid.NewGuid())
    {
        Numero = numero;
        ObjectKey = objectKey;
        FechaCargaUtc = fechaCargaUtc;
        CargadoPorSubjectId = cargadoPorSubjectId;
    }

    // Constructor para EF Core.
    private VersionInforme() { }

    public int Numero { get; private set; }

    /// <summary>Referencia al PDF en el almacenamiento de objetos (S3/MinIO, ADR-008).</summary>
    public string ObjectKey { get; private set; } = null!;

    public DateTimeOffset FechaCargaUtc { get; private set; }

    public string? CargadoPorSubjectId { get; private set; }
}
