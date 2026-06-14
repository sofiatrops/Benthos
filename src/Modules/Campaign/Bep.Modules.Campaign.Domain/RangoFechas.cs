using Bep.SharedKernel;

namespace Bep.Modules.Campaign.Domain;

/// <summary>Rango de fechas estimadas de una campaña (RF-02-001).</summary>
public sealed class RangoFechas : ValueObject
{
    private RangoFechas(DateOnly inicio, DateOnly fin)
    {
        Inicio = inicio;
        Fin = fin;
    }

    public DateOnly Inicio { get; }

    public DateOnly Fin { get; }

    public static RangoFechas Create(DateOnly inicio, DateOnly fin)
    {
        if (fin < inicio)
        {
            throw new ArgumentException("La fecha de fin no puede ser anterior a la de inicio.", nameof(fin));
        }

        return new RangoFechas(inicio, fin);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Fin;
    }
}
