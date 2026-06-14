using Bep.SharedKernel;

namespace Bep.Modules.Sampling.Domain;

/// <summary>
/// Eslabón de la cadena de custodia (RF-03-007): transferencia de responsabilidad
/// sobre una muestra, con confirmación de aceptación por el receptor.
/// </summary>
public sealed class RegistroCustodia : Entity<Guid>
{
    internal RegistroCustodia(string? deSubjectId, string paraSubjectId, DateTimeOffset fechaTransferenciaUtc)
        : base(Guid.NewGuid())
    {
        DeSubjectId = deSubjectId;
        ParaSubjectId = paraSubjectId;
        FechaTransferenciaUtc = fechaTransferenciaUtc;
        Aceptada = false;
    }

    // Constructor para EF Core.
    private RegistroCustodia() { }

    /// <summary>Sujeto que entrega (nulo si es la primera custodia, desde terreno).</summary>
    public string? DeSubjectId { get; private set; }

    /// <summary>Sujeto que recibe la responsabilidad.</summary>
    public string ParaSubjectId { get; private set; } = null!;

    public DateTimeOffset FechaTransferenciaUtc { get; private set; }

    public bool Aceptada { get; private set; }

    public DateTimeOffset? FechaAceptacionUtc { get; private set; }

    internal void Aceptar(DateTimeOffset fechaUtc)
    {
        Aceptada = true;
        FechaAceptacionUtc = fechaUtc;
    }
}
