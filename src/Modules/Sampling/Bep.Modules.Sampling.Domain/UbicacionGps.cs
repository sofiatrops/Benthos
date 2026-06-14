using Bep.SharedKernel;

namespace Bep.Modules.Sampling.Domain;

/// <summary>
/// Ubicación geográfica de captura de la muestra, con precisión del dispositivo
/// (RF-03-003). Objeto de valor independiente del de Organización (frontera de módulo).
/// </summary>
public sealed class UbicacionGps : ValueObject
{
    private UbicacionGps(double latitud, double longitud, double? precisionMetros)
    {
        Latitud = latitud;
        Longitud = longitud;
        PrecisionMetros = precisionMetros;
    }

    public double Latitud { get; }

    public double Longitud { get; }

    /// <summary>Precisión reportada por el GPS, en metros (opcional).</summary>
    public double? PrecisionMetros { get; }

    public static UbicacionGps Create(double latitud, double longitud, double? precisionMetros = null)
    {
        if (latitud is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitud), latitud, "La latitud debe estar entre -90 y 90.");
        }

        if (longitud is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitud), longitud, "La longitud debe estar entre -180 y 180.");
        }

        if (precisionMetros is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precisionMetros), precisionMetros, "La precisión no puede ser negativa.");
        }

        return new UbicacionGps(latitud, longitud, precisionMetros);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitud;
        yield return Longitud;
        yield return PrecisionMetros;
    }
}
