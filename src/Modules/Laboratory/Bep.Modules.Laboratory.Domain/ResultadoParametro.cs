using Bep.SharedKernel;

namespace Bep.Modules.Laboratory.Domain;

/// <summary>
/// Medición de un parámetro ambiental sobre una muestra, reportada por el
/// laboratorio (RF-04-002). Pertenece a un <see cref="LoteResultados"/>. La muestra
/// se referencia por su código único (MTR-…) para conservar la trazabilidad con M3.
/// </summary>
public sealed class ResultadoParametro : Entity<Guid>
{
    private ResultadoParametro(
        Guid id, string codigoMuestra, string parametro, double valor, string unidad, string? metodo)
        : base(id)
    {
        CodigoMuestra = codigoMuestra;
        Parametro = parametro;
        Valor = valor;
        Unidad = unidad;
        Metodo = metodo;
    }

    // Constructor para EF Core.
    private ResultadoParametro() { }

    /// <summary>Código único de la muestra de terreno (M3) sobre la que se midió.</summary>
    public string CodigoMuestra { get; private set; } = null!;

    public string Parametro { get; private set; } = null!;

    public double Valor { get; private set; }

    public string Unidad { get; private set; } = null!;

    public string? Metodo { get; private set; }

    public static ResultadoParametro Crear(
        string codigoMuestra, string parametro, double valor, string unidad, string? metodo)
    {
        if (string.IsNullOrWhiteSpace(codigoMuestra))
        {
            throw new ArgumentException("El resultado debe referenciar el código de la muestra.", nameof(codigoMuestra));
        }

        if (string.IsNullOrWhiteSpace(parametro))
        {
            throw new ArgumentException("El parámetro medido es obligatorio.", nameof(parametro));
        }

        if (string.IsNullOrWhiteSpace(unidad))
        {
            throw new ArgumentException("La unidad de medida es obligatoria.", nameof(unidad));
        }

        return new ResultadoParametro(
            Guid.NewGuid(), codigoMuestra.Trim(), parametro.Trim(), valor, unidad.Trim(),
            string.IsNullOrWhiteSpace(metodo) ? null : metodo.Trim());
    }
}
