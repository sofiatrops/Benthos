namespace Bep.SharedKernel;

/// <summary>
/// Objeto de Valor: inmutable y comparado por el valor de sus componentes
/// (p. ej. CoordenadasGPS, CodigoQR, RangoFechas — SRS 2.7.3).
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Componentes que definen la igualdad del objeto de valor.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
        => other is not null && GetType() == other.GetType()
           && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }
}
