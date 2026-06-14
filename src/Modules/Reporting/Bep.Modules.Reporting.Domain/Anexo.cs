using Bep.SharedKernel;

namespace Bep.Modules.Reporting.Domain;

/// <summary>Documento complementario de un informe: anexo fotográfico, datos crudos, etc. (RF-05-009).</summary>
public sealed class Anexo : Entity<Guid>
{
    internal Anexo(string objectKey, string descripcion, DateTimeOffset fechaUtc)
        : base(Guid.NewGuid())
    {
        ObjectKey = objectKey;
        Descripcion = descripcion;
        FechaUtc = fechaUtc;
    }

    // Constructor para EF Core.
    private Anexo() { }

    /// <summary>Referencia al archivo en el almacenamiento de objetos (ADR-008).</summary>
    public string ObjectKey { get; private set; } = null!;

    public string Descripcion { get; private set; } = null!;

    public DateTimeOffset FechaUtc { get; private set; }
}
