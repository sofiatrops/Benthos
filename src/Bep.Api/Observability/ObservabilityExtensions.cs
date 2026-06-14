using System.Globalization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Bep.Api.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Configura logging estructurado (Serilog) y trazas/métricas (OpenTelemetry),
    /// exportadas vía OTLP cuando hay un colector configurado (M10, RNF-FIAB-002/003).
    /// </summary>
    public static IHostApplicationBuilder AddBepObservability(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

        // Solo se exporta si hay un colector OTLP; evita ruido en desarrollo local.
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            otel.WithTracing(tracing => tracing.AddOtlpExporter())
                .WithMetrics(metrics => metrics.AddOtlpExporter());
        }

        return builder;
    }
}
