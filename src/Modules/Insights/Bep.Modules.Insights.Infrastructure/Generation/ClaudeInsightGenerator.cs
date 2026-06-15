using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bep.Modules.Insights.Application.Generation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bep.Modules.Insights.Infrastructure.Generation;

/// <summary>
/// Adaptador de un LLM comercial (Claude / Anthropic Messages API) para generar el
/// análisis ambiental (ADR-006). Solo se le envían estadísticas agregadas y
/// de-identificadas. Pide al modelo una respuesta JSON estructurada y la valida.
/// Se activa con <c>Insights:Provider=claude</c> y una clave; en otro caso se usa el
/// generador determinista. La validación profesional posterior sigue siendo obligatoria.
/// </summary>
internal sealed class ClaudeInsightGenerator(
    HttpClient httpClient,
    IOptions<InsightsOptions> options,
    ILogger<ClaudeInsightGenerator> logger) : IGeneradorAnalisis
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly InsightsOptions _options = options.Value;

    public string Modelo => _options.Modelo;

    public async Task<AnalisisIaResultado> GenerarAsync(ContextoAnalisis contexto, CancellationToken cancellationToken)
    {
        const string system =
            "Eres un analista ambiental. A partir de estadísticas agregadas de parámetros de monitoreo, " +
            "redacta un análisis profesional en español. Responde EXCLUSIVAMENTE con un objeto JSON: " +
            "{\"resumen\": string, \"hallazgos\": [{\"parametro\": string, \"severidad\": " +
            "\"Informativo\"|\"Atencion\"|\"Critico\", \"detalle\": string}]}. Sin texto fuera del JSON.";

        var datos = JsonSerializer.Serialize(contexto.Parametros, JsonOptions);
        var request = new AnthropicRequest(
            _options.Modelo,
            _options.MaxTokens,
            system,
            [new AnthropicMessage("user", $"Parámetros de la campaña (JSON):\n{datos}")]);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl)
        {
            Content = JsonContent.Create(request),
        };
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AnthropicResponse>(JsonOptions, cancellationToken);
        var texto = payload?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new InvalidOperationException("La respuesta del modelo no contiene texto.");
        }

        return Parsear(texto, logger);
    }

    private static AnalisisIaResultado Parsear(string texto, ILogger logger)
    {
        // El modelo puede envolver el JSON en vallas de código; recortamos al objeto.
        var inicio = texto.IndexOf('{', StringComparison.Ordinal);
        var fin = texto.LastIndexOf('}');
        if (inicio < 0 || fin <= inicio)
        {
            throw new InvalidOperationException("La respuesta del modelo no contiene un JSON válido.");
        }

        var json = texto[inicio..(fin + 1)];
        try
        {
            var generado = JsonSerializer.Deserialize<AnalisisIaResultado>(json, JsonOptions);
            if (generado is null || string.IsNullOrWhiteSpace(generado.Resumen))
            {
                throw new InvalidOperationException("La respuesta del modelo no incluye un resumen.");
            }

            return generado with { Hallazgos = generado.Hallazgos ?? [] };
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "No se pudo parsear la respuesta JSON del modelo.");
            throw new InvalidOperationException("No se pudo interpretar la respuesta del modelo.", ex);
        }
    }

    private sealed record AnthropicRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] IReadOnlyList<AnthropicMessage> Messages);

    private sealed record AnthropicMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record AnthropicResponse(
        [property: JsonPropertyName("content")] IReadOnlyList<AnthropicContent>? Content);

    private sealed record AnthropicContent(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
