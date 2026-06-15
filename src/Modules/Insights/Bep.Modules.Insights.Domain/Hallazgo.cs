using Bep.SharedKernel;

namespace Bep.Modules.Insights.Domain;

/// <summary>
/// Hallazgo puntual del análisis ambiental (p. ej. un parámetro fuera de rango
/// esperado). Pertenece a un <see cref="AnalisisAmbiental"/>.
/// </summary>
public sealed class Hallazgo : Entity<Guid>
{
    private Hallazgo(Guid id, string parametro, SeveridadHallazgo severidad, string detalle) : base(id)
    {
        Parametro = parametro;
        Severidad = severidad;
        Detalle = detalle;
    }

    // Constructor para EF Core.
    private Hallazgo() { }

    public string Parametro { get; private set; } = null!;

    public SeveridadHallazgo Severidad { get; private set; }

    public string Detalle { get; private set; } = null!;

    public static Hallazgo Crear(string parametro, SeveridadHallazgo severidad, string detalle)
    {
        if (string.IsNullOrWhiteSpace(parametro))
        {
            throw new ArgumentException("El hallazgo debe indicar el parámetro.", nameof(parametro));
        }

        if (string.IsNullOrWhiteSpace(detalle))
        {
            throw new ArgumentException("El hallazgo debe incluir un detalle.", nameof(detalle));
        }

        return new Hallazgo(Guid.NewGuid(), parametro.Trim(), severidad, detalle.Trim());
    }
}
