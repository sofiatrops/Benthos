using Bep.SharedKernel;

namespace Bep.Modules.Sampling.Domain;

/// <summary>
/// Evento del historial cronológico de una muestra (RF-03-006). Entidad hija del
/// agregado <see cref="Muestra"/>; se agrega, nunca se modifica.
/// </summary>
public sealed class EventoMuestra : Entity<Guid>
{
    internal EventoMuestra(TipoEventoMuestra tipo, DateTimeOffset fechaUtc, string? usuarioSubjectId, string descripcion)
        : base(Guid.NewGuid())
    {
        Tipo = tipo;
        FechaUtc = fechaUtc;
        UsuarioSubjectId = usuarioSubjectId;
        Descripcion = descripcion;
    }

    // Constructor para EF Core.
    private EventoMuestra() { }

    public TipoEventoMuestra Tipo { get; private set; }

    public DateTimeOffset FechaUtc { get; private set; }

    public string? UsuarioSubjectId { get; private set; }

    public string Descripcion { get; private set; } = null!;
}
