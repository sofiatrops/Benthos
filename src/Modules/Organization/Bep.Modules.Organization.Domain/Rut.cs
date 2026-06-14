using Bep.SharedKernel;

namespace Bep.Modules.Organization.Domain;

/// <summary>
/// RUT / identificador fiscal de una empresa (RF-01-001). Objeto de valor que
/// normaliza el formato y valida el dígito verificador (algoritmo módulo 11).
/// </summary>
public sealed class Rut : ValueObject
{
    private Rut(string value) => Value = value;

    public string Value { get; }

    public static Rut Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("El RUT no puede estar vacío.", nameof(raw));
        }

        var normalized = raw.Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

        if (normalized.Length < 2)
        {
            throw new ArgumentException("El RUT no tiene el formato esperado.", nameof(raw));
        }

        var body = normalized[..^1];
        var checkDigit = normalized[^1];

        if (!body.All(char.IsDigit))
        {
            throw new ArgumentException("El cuerpo del RUT debe ser numérico.", nameof(raw));
        }

        if (ComputeCheckDigit(body) != checkDigit)
        {
            throw new ArgumentException("El dígito verificador del RUT no es válido.", nameof(raw));
        }

        return new Rut($"{body}-{checkDigit}");
    }

    private static char ComputeCheckDigit(string body)
    {
        var sum = 0;
        var multiplier = 2;

        for (var i = body.Length - 1; i >= 0; i--)
        {
            sum += (body[i] - '0') * multiplier;
            multiplier = multiplier == 7 ? 2 : multiplier + 1;
        }

        var remainder = 11 - (sum % 11);
        return remainder switch
        {
            11 => '0',
            10 => 'K',
            _ => (char)('0' + remainder),
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
