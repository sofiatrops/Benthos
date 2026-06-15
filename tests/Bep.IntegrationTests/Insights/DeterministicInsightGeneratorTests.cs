using Bep.Modules.Insights.Application.Generation;
using Bep.Modules.Insights.Infrastructure.Generation;

namespace Bep.IntegrationTests.Insights;

/// <summary>
/// Pruebas del generador determinista (proveedor por defecto, sin servicio externo):
/// produce un resumen no vacío y un hallazgo por parámetro, marcando la alta
/// variabilidad como atención.
/// </summary>
public sealed class DeterministicInsightGeneratorTests
{
    private static readonly DeterministicInsightGenerator Generador = new();

    [Fact]
    public async Task Genera_resumen_y_un_hallazgo_por_parametro()
    {
        var contexto = new ContextoAnalisis(Guid.NewGuid(),
        [
            new ParametroResumen("pH", "pH", 4, 7.8, 8.0, 7.9),
            new ParametroResumen("Oxígeno disuelto", "mg/L", 4, 7.0, 9.0, 8.0),
        ]);

        var resultado = await Generador.GenerarAsync(contexto, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(resultado.Resumen));
        Assert.Equal(2, resultado.Hallazgos.Count);
        Assert.Equal("deterministic-v1", Generador.Modelo);
    }

    [Fact]
    public async Task Marca_atencion_cuando_la_variabilidad_es_alta()
    {
        var contexto = new ContextoAnalisis(Guid.NewGuid(),
        [
            // Rango (1..10) muy amplio frente al promedio (5) → variabilidad alta.
            new ParametroResumen("Turbidez", "NTU", 5, 1, 10, 5),
        ]);

        var resultado = await Generador.GenerarAsync(contexto, CancellationToken.None);

        Assert.Equal("Atencion", resultado.Hallazgos[0].Severidad);
    }
}
