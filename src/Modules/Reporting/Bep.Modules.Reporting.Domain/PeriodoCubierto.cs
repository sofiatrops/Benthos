using Bep.SharedKernel;

namespace Bep.Modules.Reporting.Domain;

/// <summary>Período temporal que cubre el informe (RF-05-006).</summary>
public sealed class PeriodoCubierto : ValueObject
{
    private PeriodoCubierto(DateOnly desde, DateOnly hasta)
    {
        Desde = desde;
        Hasta = hasta;
    }

    public DateOnly Desde { get; }

    public DateOnly Hasta { get; }

    public static PeriodoCubierto Create(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
        {
            throw new ArgumentException("La fecha final del período no puede ser anterior a la inicial.", nameof(hasta));
        }

        return new PeriodoCubierto(desde, hasta);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Desde;
        yield return Hasta;
    }
}
