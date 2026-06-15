using System.Globalization;
using System.Text;
using Bep.Modules.Insights.Application.Generation;

namespace Bep.Modules.Insights.Infrastructure.Generation;

/// <summary>
/// Generador determinista de análisis: redacta un resumen y hallazgos a partir de
/// las estadísticas de parámetros, sin servicio externo. Es el proveedor por defecto
/// (gratuito, reproducible, sin envío de datos) y la red de seguridad si el LLM no
/// está configurado (ADR-006).
/// </summary>
public sealed class DeterministicInsightGenerator : IGeneradorAnalisis
{
    public string Modelo => "deterministic-v1";

    public Task<AnalisisIaResultado> GenerarAsync(ContextoAnalisis contexto, CancellationToken cancellationToken)
    {
        var totalMediciones = contexto.Parametros.Sum(p => p.N);
        var resumen = new StringBuilder()
            .Append(CultureInfo.GetCultureInfo("es-CL"),
                $"Se analizaron {contexto.Parametros.Count} parámetro(s) sobre {totalMediciones} medición(es) validada(s). ")
            .Append("Resumen por parámetro: ")
            .AppendJoin("; ", contexto.Parametros.Select(p =>
                $"{p.Parametro} promedió {Fmt(p.Promedio)} {p.Unidad} (rango {Fmt(p.Min)}–{Fmt(p.Max)}, n={p.N})"))
            .Append('.')
            .ToString();

        var hallazgos = new List<HallazgoIa>();
        foreach (var p in contexto.Parametros)
        {
            // Heurística simple y transparente: marca alta variabilidad relativa.
            var rango = p.Max - p.Min;
            var variabilidadAlta = p.Promedio != 0 && rango / Math.Abs(p.Promedio) > 0.5;

            hallazgos.Add(variabilidadAlta
                ? new HallazgoIa(p.Parametro, nameof(Domain.SeveridadHallazgo.Atencion),
                    $"Variabilidad alta en {p.Parametro}: rango {Fmt(p.Min)}–{Fmt(p.Max)} {p.Unidad} frente a un promedio de {Fmt(p.Promedio)}.")
                : new HallazgoIa(p.Parametro, nameof(Domain.SeveridadHallazgo.Informativo),
                    $"{p.Parametro} estable alrededor de {Fmt(p.Promedio)} {p.Unidad}."));
        }

        return Task.FromResult(new AnalisisIaResultado(resumen, hallazgos));
    }

    private static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
