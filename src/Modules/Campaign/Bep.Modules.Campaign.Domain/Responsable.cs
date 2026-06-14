using Bep.SharedKernel;

namespace Bep.Modules.Campaign.Domain;

/// <summary>
/// Responsable asignado a una campaña (RF-02-002): el sujeto (usuario de Benthos)
/// y su rol dentro de la campaña (coordinador o técnico de terreno).
/// </summary>
public sealed class Responsable : ValueObject
{
    private Responsable(string subjectId, string rol)
    {
        SubjectId = subjectId;
        Rol = rol;
    }

    public string SubjectId { get; private set; } = null!;

    public string Rol { get; private set; } = null!;

    // Constructor para el materializador de EF (OwnsMany/ToJson).
    private Responsable() { }

    public static Responsable Create(string subjectId, string rol)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new ArgumentException("El responsable debe tener un sujeto.", nameof(subjectId));
        }

        if (string.IsNullOrWhiteSpace(rol))
        {
            throw new ArgumentException("El responsable debe tener un rol.", nameof(rol));
        }

        return new Responsable(subjectId.Trim(), rol.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SubjectId;
        yield return Rol;
    }
}
