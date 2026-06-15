using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Storage;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Campaign.Application.Campanias.CrearCampana;
using Bep.Modules.Campaign.Application.Campanias.TransicionarEstado;
using Bep.Modules.Campaign.Domain;
using Bep.Modules.Campaign.Infrastructure;
using Bep.Modules.Campaign.Infrastructure.Persistence;
using Bep.Modules.Insights.Infrastructure;
using Bep.Modules.Insights.Infrastructure.Persistence;
using Bep.Modules.Laboratory.Infrastructure;
using Bep.Modules.Laboratory.Infrastructure.Persistence;
using Bep.Modules.Portal.Application;
using Bep.Modules.Portal.Application.Dashboard;
using Bep.Modules.Portal.Application.Informes;
using Bep.Modules.Reporting.Application.Informes;
using Bep.Modules.Reporting.Application.Informes.CrearInforme;
using Bep.Modules.Reporting.Domain;
using Bep.Modules.Reporting.Infrastructure;
using Bep.Modules.Reporting.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Portal;

/// <summary>
/// Flujo del Portal Cliente (M7). Verifica el dashboard, el listado de publicados,
/// el detalle descargable sin comentarios internos, y sobre todo el aislamiento
/// estricto por tenant derivado del JWT (RF-07-010): un cliente no puede acceder a
/// datos de otra empresa ni a informes no publicados.
/// </summary>
public sealed class PortalFlowTests : IAsyncLifetime
{
    private const string AppRole = "bep_app";
    private const string AppPassword = "bep_app_test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("bep").WithUsername("postgres").WithPassword("postgres").Build();

    private readonly Guid _empresaA = Guid.NewGuid();
    private readonly Guid _empresaB = Guid.NewGuid();

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var adminConnectionString = _postgres.GetConnectionString();
        var appConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Username = AppRole,
            Password = AppPassword,
        }.ConnectionString;

        await MigrarAsync<CampaignDbContext>(adminConnectionString, o => new CampaignDbContext(o, new NullTenantContext()));
        await MigrarAsync<ReportingDbContext>(adminConnectionString, o => new ReportingDbContext(o, new NullTenantContext()));
        await MigrarAsync<LaboratoryDbContext>(adminConnectionString, o => new LaboratoryDbContext(o, new NullTenantContext()));
        await MigrarAsync<InsightsDbContext>(adminConnectionString, o => new InsightsDbContext(o, new NullTenantContext()));

        await ExecuteAdminSqlAsync(adminConnectionString, $"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA campaign, reporting, laboratory, insights TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA campaign TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA reporting TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA laboratory TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA insights TO {AppRole};
            """);

        // El usuario autenticado del Portal es un cliente del tenant A.
        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddScoped<ICurrentUser>(_ => new StubClientUser(_empresaA))
            .AddSingleton<IObjectStorage, StubObjectStorage>()
            .AddCampaignModule(appConnectionString)
            .AddReportingModule(appConnectionString)
            .AddLaboratoryModule(appConnectionString)
            .AddInsightsModule(appConnectionString, new ConfigurationBuilder().Build())
            .AddPortalApplication()
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Dashboard_shows_active_campaigns_and_latest_published_reports()
    {
        await CrearCampanaActivaAsync(_empresaA);
        await CrearCampanaActivaAsync(_empresaA);
        await CerrarCampanaAsync(_empresaA);

        await PublicarInformeAsync(_empresaA, "Informe publicado 1");
        await PublicarInformeAsync(_empresaA, "Informe publicado 2");
        await CrearInformeBorradorAsync(_empresaA, "Borrador");

        using var scope = _provider.CreateScope();
        var dashboard = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PortalDashboardQuery());

        Assert.True(dashboard.IsSuccess);
        Assert.Equal(2, dashboard.Value.CampanasActivas);
        Assert.Equal(2, dashboard.Value.UltimosInformesPublicados.Count);
    }

    [Fact]
    public async Task List_returns_only_published_reports()
    {
        await PublicarInformeAsync(_empresaA, "Publicado");
        await CrearInformeBorradorAsync(_empresaA, "Borrador");

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new PortalListarInformesPublicadosQuery());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task Report_detail_is_downloadable_without_internal_comments()
    {
        var informeId = await PublicarInformeAsync(_empresaA, "Con comentario", agregarComentario: true);

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new PortalObtenerInformeQuery(informeId));

        Assert.True(result.IsSuccess);
        // URL de descarga firmada de la versión vigente (RF-07-004 / ADR-008).
        Assert.NotNull(result.Value.UrlDescarga);
        // El DTO del portal no tiene comentarios internos (RF-05-004): garantía de tipo.
        Assert.IsType<InformePublicadoDetalleDto>(result.Value);
    }

    [Fact]
    public async Task Client_cannot_access_another_tenant_report_RF_07_010()
    {
        // Informe publicado del tenant B.
        var informeB = await PublicarInformeAsync(_empresaB, "Confidencial de B");

        // El cliente del tenant A intenta obtenerlo por id: RLS lo hace invisible.
        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new PortalObtenerInformeQuery(informeB));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Client_cannot_access_unpublished_report()
    {
        var borradorId = await CrearInformeBorradorAsync(_empresaA, "Borrador no publicado");

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new PortalObtenerInformeQuery(borradorId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    // --- Helpers de preparación (actúan como personal de Benthos vía empresaId explícito) ---

    private async Task CrearCampanaActivaAsync(Guid empresaId)
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CrearCampanaCommand(
            empresaId, "Campaña activa", "desc", TipoCampania.Mixta,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), [Guid.NewGuid()]));
    }

    private async Task CerrarCampanaAsync(Guid empresaId)
    {
        Guid campanaId;
        using (var scope = _provider.CreateScope())
        {
            var r = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CrearCampanaCommand(
                empresaId, "Campaña a cerrar", "desc", TipoCampania.Mixta,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), [Guid.NewGuid()]));
            campanaId = r.Value;
        }

        await TransicionarCampanaAsync(empresaId, campanaId, EstadoCampania.EnCurso);
        await TransicionarCampanaAsync(empresaId, campanaId, EstadoCampania.EnRevision);
        await TransicionarCampanaAsync(empresaId, campanaId, EstadoCampania.Cerrada);
    }

    private async Task TransicionarCampanaAsync(Guid empresaId, Guid campanaId, EstadoCampania estado)
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new TransicionarEstadoCommand(empresaId, campanaId, estado));
    }

    private async Task<Guid> CrearInformeBorradorAsync(Guid empresaId, string titulo)
    {
        using var scope = _provider.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CrearInformeCommand(
            empresaId, titulo, TipoEstudio.CalidadAgua,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), null, null, $"{empresaId:D}/informes/v1.pdf"));
        return r.Value;
    }

    private async Task<Guid> PublicarInformeAsync(Guid empresaId, string titulo, bool agregarComentario = false)
    {
        var informeId = await CrearInformeBorradorAsync(empresaId, titulo);

        if (agregarComentario)
        {
            using var scope = _provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new AgregarComentarioCommand(empresaId, informeId, "Comentario interno de revisión."));
        }

        await TransicionarInformeAsync(empresaId, informeId, EstadoInforme.EnRevision);
        await TransicionarInformeAsync(empresaId, informeId, EstadoInforme.Aprobado);
        await TransicionarInformeAsync(empresaId, informeId, EstadoInforme.Publicado);
        return informeId;
    }

    private async Task TransicionarInformeAsync(Guid empresaId, Guid informeId, EstadoInforme estado)
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new TransicionarEstadoInformeCommand(empresaId, informeId, estado));
    }

    private static async Task MigrarAsync<TContext>(string connectionString, Func<DbContextOptions<TContext>, TContext> factory)
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>().UseNpgsql(connectionString).Options;
        await using var context = factory(options);
        await context.Database.MigrateAsync();
    }

    private static async Task ExecuteAdminSqlAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private sealed class NullTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public bool HasTenant => false;
        public void SetTenant(Guid tenantId) { }
    }

    private sealed class StubClientUser(Guid tenantId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? SubjectId => "cliente-a";
        public PrincipalType PrincipalType => PrincipalType.ClientUser;
        public Guid? TenantId => tenantId;
        public IReadOnlyCollection<string> Roles => ["usuario-cliente"];
    }

    /// <summary>Almacenamiento de prueba: devuelve una URL determinista sin tocar S3.</summary>
    private sealed class StubObjectStorage : IObjectStorage
    {
        public Task<Result<UploadTicket>> CrearTicketSubidaAsync(SolicitudSubida solicitud, CancellationToken ct)
            => Task.FromResult(Result.Success(new UploadTicket(
                "key", new Uri("https://storage.local/upload"), solicitud.ContentType, DateTimeOffset.UtcNow.AddMinutes(10))));

        public Task<Result<Uri>> CrearUrlDescargaAsync(string objectKey, CancellationToken ct)
            => Task.FromResult(Result.Success(new Uri($"https://storage.local/{objectKey}")));

        public Task<Result<Stream>> AbrirLecturaAsync(string objectKey, CancellationToken ct)
            => Task.FromResult(Result.Success<Stream>(new MemoryStream()));
    }
}
