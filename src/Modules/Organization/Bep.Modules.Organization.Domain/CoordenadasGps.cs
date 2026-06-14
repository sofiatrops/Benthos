using Bep.SharedKernel;

namespace Bep.Modules.Organization.Domain;

/// <summary>
/// Coordenadas geográficas de un centro (RF-01-002). Objeto de valor con
/// validación de rangos. En persistencia se proyecta a un punto PostGIS
/// (SRS 2.8.3) para consultas espaciales.
/// </summary>
public sealed class CoordenadasGps : ValueObject
{
    private CoordenadasGps(double latitud, double longitud)
    {
        Latitud = latitud;
        Longitud = longitud;
    }

    public double Latitud { get; }

    public double Longitud { get; }

    public static CoordenadasGps Create(double latitud, double longitud)
    {
        if (latitud is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitud), latitud, "La latitud debe estar entre -90 y 90.");
        }

        if (longitud is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitud), longitud, "La longitud debe estar entre -180 y 180.");
        }

        return new CoordenadasGps(latitud, longitud);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitud;
        yield return Longitud;
    }
}
