using Bep.SharedKernel;

namespace Bep.Modules.Sampling.Domain;

/// <summary>
/// Token único que codifica el código QR de una muestra (RF-03-002). Es lo que se
/// imprime/escanea para consultar el estado y la ubicación de la muestra (RF-03-008).
/// La generación de la imagen QR es responsabilidad de la capa de presentación.
/// </summary>
public sealed class CodigoQr : ValueObject
{
    private CodigoQr(string value) => Value = value;

    public string Value { get; }

    public static CodigoQr Generar() => new($"QR-{Guid.NewGuid():N}");

    public static CodigoQr Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El código QR no puede estar vacío.", nameof(value));
        }

        return new CodigoQr(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
