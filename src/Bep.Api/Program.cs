using Bep.Api.Authentication;
using Bep.Api.Endpoints;
using Bep.Api.HealthChecks;
using Bep.Api.Http;
using Bep.Api.Observability;
using Bep.Api.Tenancy;
using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Infrastructure.Storage;
using Bep.Modules.Audit.Infrastructure;
using Bep.Modules.Audit.Infrastructure.Persistence;
using Bep.Modules.Campaign.Infrastructure;
using Bep.Modules.Campaign.Infrastructure.Persistence;
using Bep.Modules.Insights.Infrastructure;
using Bep.Modules.Insights.Infrastructure.Persistence;
using Bep.Modules.Laboratory.Infrastructure;
using Bep.Modules.Laboratory.Infrastructure.Persistence;
using Bep.Modules.Organization.Infrastructure;
using Bep.Modules.Organization.Infrastructure.Persistence;
using Bep.Modules.Portal.Application;
using Bep.Modules.Reporting.Infrastructure;
using Bep.Modules.Reporting.Infrastructure.Persistence;
using Bep.Modules.Sampling.Infrastructure;
using Bep.Modules.Sampling.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddBepObservability(serviceName: "bep-api");

var connectionString = builder.Configuration.GetConnectionString("Bep")
    ?? Environment.GetEnvironmentVariable("BEP_DB_CONNECTION")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Bep' (ConnectionStrings:Bep o BEP_DB_CONNECTION).");

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Enums como texto en JSON (peticiones y respuestas): API más ergonómica para el
// frontend (p. ej. tipo de campaña "Mixta" en lugar de un número).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// CORS para la SPA del Portal (Angular). Orígenes permitidos por configuración;
// por defecto el dev server local. El token va en cabecera Bearer (sin cookies).
const string spaCorsPolicy = "bep-spa";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddPolicy(spaCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddBepAuthentication(builder.Configuration);
builder.Services.AddBepAuthorization();

// Aislamiento multi-tenant: contexto de tenant + interceptor RLS (ADR-004).
builder.Services.AddBepTenancy();

// Almacenamiento de objetos S3-compatible con URLs firmadas (ADR-008).
builder.Services.AddBepObjectStorage(builder.Configuration);

// Auditoría persistente por eventos de dominio (M8).
builder.Services.AddAuditModule(connectionString);

// Módulos de negocio.
builder.Services.AddOrganizationModule(connectionString);
builder.Services.AddCampaignModule(connectionString);
builder.Services.AddSamplingModule(connectionString);
builder.Services.AddReportingModule(connectionString);
builder.Services.AddLaboratoryModule(connectionString);
builder.Services.AddInsightsModule(connectionString, builder.Configuration);

// M7 Portal Cliente: agrega lecturas de Campañas e Informes (sin persistencia propia).
builder.Services.AddPortalApplication();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    // Conveniencia de desarrollo: aplicar migraciones al arrancar. En producción
    // las migraciones se ejecutan en el pipeline de despliegue (RNF-DATOS-006).
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<CampaignDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<SamplingDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ReportingDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<LaboratoryDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<InsightsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AuditDbContext>().Database.MigrateAsync();

        // Datos de demostración para el Portal (idempotente, solo Development).
        var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevDataSeeder");
        await Bep.Api.DevData.DevDataSeeder.SeedAsync(scope.ServiceProvider, seedLogger);
    }

    app.MapOpenApi();
}

// Cabeceras de seguridad HTTP estándar (RNF-SEG-010).
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});

app.UseHttpsRedirection();

app.UseCors(spaCorsPolicy);

app.UseAuthentication();
app.UseBepTenantResolution();
app.UseAuthorization();

// Liveness: el proceso responde. Readiness: dependencias críticas accesibles.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

// Endpoint de identidad: confirma autenticación y contexto de tenant resuelto.
app.MapGet("/api/v1/me", (ICurrentUser currentUser, ITenantContext tenantContext) => Results.Ok(new
{
    currentUser.IsAuthenticated,
    currentUser.SubjectId,
    PrincipalType = currentUser.PrincipalType.ToString(),
    currentUser.TenantId,
    EffectiveTenantId = tenantContext.TenantId,
    currentUser.Roles,
}))
.RequireAuthorization()
.WithName("GetCurrentUser");

app.MapOrganizationEndpoints();
app.MapCampaignEndpoints();
app.MapSamplingEndpoints();
app.MapReportingEndpoints();
app.MapLaboratoryEndpoints();
app.MapInsightsEndpoints();
app.MapPortalEndpoints();
app.MapStorageEndpoints();

app.Run();

/// <summary>Punto de entrada expuesto para pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
