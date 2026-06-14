using System.Globalization;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Logging estructurado (M10). El Worker comparte convenciones con la API.
builder.Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

var connectionString = builder.Configuration.GetConnectionString("Bep")
    ?? Environment.GetEnvironmentVariable("BEP_DB_CONNECTION")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Bep' (ConnectionStrings:Bep o BEP_DB_CONNECTION).");

// Trabajos en segundo plano durables sobre PostgreSQL (ADR-005): importación de
// laboratorio, procesamiento de IA, reportes grandes. Proceso separado de la API
// para mantenerla stateless (RNF-ESC-001/004).
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer(options => options.ServerName = "bep-worker");

var host = builder.Build();
host.Run();
