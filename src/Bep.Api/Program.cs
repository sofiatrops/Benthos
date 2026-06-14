using Bep.Api.Authentication;
using Bep.Api.Endpoints;
using Bep.Api.HealthChecks;
using Bep.Api.Http;
using Bep.Api.Observability;
using Bep.Api.Tenancy;
using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Audit.Infrastructure;
using Bep.Modules.Audit.Infrastructure.Persistence;
using Bep.Modules.Campaign.Infrastructure;
using Bep.Modules.Campaign.Infrastructure.Persistence;
using Bep.Modules.Organization.Infrastructure;
using Bep.Modules.Organization.Infrastructure.Persistence;
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
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddBepAuthentication(builder.Configuration);
builder.Services.AddBepAuthorization();

// Aislamiento multi-tenant: contexto de tenant + interceptor RLS (ADR-004).
builder.Services.AddBepTenancy();

// Auditoría persistente por eventos de dominio (M8).
builder.Services.AddAuditModule(connectionString);

// Módulos de negocio.
builder.Services.AddOrganizationModule(connectionString);
builder.Services.AddCampaignModule(connectionString);
builder.Services.AddSamplingModule(connectionString);

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
        await scope.ServiceProvider.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
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

app.Run();

/// <summary>Punto de entrada expuesto para pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
