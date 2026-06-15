namespace Bep.Modules.Insights.Infrastructure.Generation;

/// <summary>
/// Configuración del generador de análisis de IA (ADR-006). Por defecto usa el
/// proveedor <c>deterministic</c> (sin servicio externo, sin costo ni envío de datos).
/// El proveedor <c>claude</c> activa un LLM comercial (requiere clave y, en producción,
/// DPA); los secretos llegan por entorno, nunca versionados (RNF-SEG-004).
/// </summary>
public sealed class InsightsOptions
{
    public const string SectionName = "Insights";

    /// <summary><c>deterministic</c> (por defecto) o <c>claude</c>.</summary>
    public string Provider { get; set; } = "deterministic";

    public string ApiKey { get; set; } = string.Empty;

    public string Modelo { get; set; } = "claude-sonnet-4-6";

    public string ApiUrl { get; set; } = "https://api.anthropic.com/v1/messages";

    public int MaxTokens { get; set; } = 1024;
}
