using Bep.SharedKernel;

namespace Bep.Modules.Reporting.Domain;

/// <summary>
/// Comentario interno de revisión, visible solo para personal de Benthos y nunca
/// para el cliente (RF-05-004).
/// </summary>
public sealed class ComentarioInterno : Entity<Guid>
{
    internal ComentarioInterno(string autorSubjectId, string texto, DateTimeOffset fechaUtc)
        : base(Guid.NewGuid())
    {
        AutorSubjectId = autorSubjectId;
        Texto = texto;
        FechaUtc = fechaUtc;
    }

    // Constructor para EF Core.
    private ComentarioInterno() { }

    public string AutorSubjectId { get; private set; } = null!;

    public string Texto { get; private set; } = null!;

    public DateTimeOffset FechaUtc { get; private set; }
}
