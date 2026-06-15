using System.Globalization;

namespace Bep.Modules.Laboratory.Application.Parsing;

/// <summary>Una medición leída del archivo, aún sin construir el agregado.</summary>
public sealed record ResultadoParametroDraft(
    string CodigoMuestra, string Parametro, double Valor, string Unidad, string? Metodo);

/// <summary>Resultado de parsear un archivo: filas válidas y errores por fila (no abortan el lote).</summary>
public sealed record ParseResultado(
    IReadOnlyList<ResultadoParametroDraft> Resultados, IReadOnlyList<string> Errores);

/// <summary>
/// Estrategia de lectura de un formato de archivo de resultados (RF-04-001). Permite
/// sumar adaptadores por formato (CSV hoy; Excel y APIs de laboratorio después) sin
/// tocar la orquestación de ingesta (patrón Strategy/Adapter).
/// </summary>
public interface IResultadosParser
{
    /// <summary>Identificador del formato soportado (p. ej. <c>csv</c>).</summary>
    public string Formato { get; }

    public ParseResultado Parse(Stream contenido);
}

/// <summary>
/// Parser CSV con cabecera <c>codigo_muestra,parametro,valor,unidad,metodo</c>
/// (la columna <c>metodo</c> es opcional). Las filas malformadas se omiten y se
/// reportan como errores, sin invalidar el resto del lote.
/// </summary>
public sealed class CsvResultadosParser : IResultadosParser
{
    public string Formato => "csv";

    public ParseResultado Parse(Stream contenido)
    {
        var resultados = new List<ResultadoParametroDraft>();
        var errores = new List<string>();

        using var reader = new StreamReader(contenido);
        var contenidoTexto = reader.ReadToEnd();
        var lineas = contenidoTexto.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lineas.Length <= 1)
        {
            errores.Add("El archivo no contiene filas de datos.");
            return new ParseResultado(resultados, errores);
        }

        // La primera línea es la cabecera; se ignora.
        for (var i = 1; i < lineas.Length; i++)
        {
            var numeroFila = i + 1;
            var campos = lineas[i].Trim('\r', ' ').Split(',');

            if (campos.Length < 4)
            {
                errores.Add($"Fila {numeroFila}: se esperaban al menos 4 columnas.");
                continue;
            }

            var codigoMuestra = campos[0].Trim();
            var parametro = campos[1].Trim();
            var valorTexto = campos[2].Trim();
            var unidad = campos[3].Trim();
            var metodo = campos.Length > 4 ? campos[4].Trim() : null;

            if (string.IsNullOrWhiteSpace(codigoMuestra) || string.IsNullOrWhiteSpace(parametro) || string.IsNullOrWhiteSpace(unidad))
            {
                errores.Add($"Fila {numeroFila}: código de muestra, parámetro y unidad son obligatorios.");
                continue;
            }

            if (!double.TryParse(valorTexto, NumberStyles.Float, CultureInfo.InvariantCulture, out var valor))
            {
                errores.Add($"Fila {numeroFila}: el valor '{valorTexto}' no es numérico.");
                continue;
            }

            resultados.Add(new ResultadoParametroDraft(codigoMuestra, parametro, valor, unidad, metodo));
        }

        return new ParseResultado(resultados, errores);
    }
}
