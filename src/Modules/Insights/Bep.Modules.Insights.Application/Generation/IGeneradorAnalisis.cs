namespace Bep.Modules.Insights.Application.Generation;

/// <summary>Resumen estadístico de un parámetro medido (insumo de-identificado para el generador).</summary>
public sealed record ParametroResumen(string Parametro, string Unidad, int N, double Min, double Max, double Promedio);

/// <summary>
/// Contexto que se entrega al generador de análisis. Por gobierno de datos (ADR-006)
/// contiene <b>solo estadísticas agregadas y de-identificadas</b> de parámetros: ni
/// razón social, ni texto crudo de informes, ni datos personales.
/// </summary>
public sealed record ContextoAnalisis(Guid CampanaId, IReadOnlyList<ParametroResumen> Parametros);

/// <summary>Hallazgo propuesto por el generador (la severidad se valida contra el dominio).</summary>
public sealed record HallazgoIa(string Parametro, string Severidad, string Detalle);

/// <summary>Salida del generador: resumen en lenguaje natural y hallazgos.</summary>
public sealed record AnalisisIaResultado(string Resumen, IReadOnlyList<HallazgoIa> Hallazgos);

/// <summary>
/// Estrategia de generación de análisis ambiental (RF-06). Permite intercambiar el
/// proveedor —determinista (sin servicio externo) o un LLM comercial con DPA— sin
/// tocar la orquestación ni el flujo de validación humana (ADR-006).
/// </summary>
public interface IGeneradorAnalisis
{
    /// <summary>Identificador del modelo/proveedor, que se persiste para trazabilidad.</summary>
    public string Modelo { get; }

    public Task<AnalisisIaResultado> GenerarAsync(ContextoAnalisis contexto, CancellationToken cancellationToken);
}
