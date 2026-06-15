using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Audit.Infrastructure;
using Bep.Modules.Audit.Infrastructure.Persistence;
using Bep.Modules.Reporting.Application.Informes;
using Bep.Modules.Reporting.Application.Informes.CrearInforme;
using Bep.Modules.Reporting.Application.Informes.Queries;
using Bep.Modules.Reporting.Domain;
using Bep.Modules.Reporting.Infrastructure;
using Bep.Modules.Reporting.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Reporting;

/// <summary>
/// Flujo vertical de M5: crear informe con versionado, flujo de revisión hasta
/// publicado (auditado), visibilidad de publicados (RF-05-005), archivado lógico
/// (RF-05-010) y aislamiento por tenant (RLS).
/// </summary>
public sealed class ReportingFlowTests : IAsyncLifetime
{
    private const string AppRole = "bep_app";
    private const string AppPassword = "bep_app_test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("bep")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

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

        await using (var reporting = new ReportingDbContext(
            new DbContextOptionsBuilder<ReportingDbContext>().UseNpgsql(adminConnectionString).Options,
            new NullTenantContext()))
        {
            await reporting.Database.MigrateAsync();
        }

        await using (var audit = new AuditDbContext(
            new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(adminConnectionString).Options))
        {
            await audit.Database.MigrateAsync();
        }

        await ExecuteAdminSqlAsync(adminConnectionString, $"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA reporting, audit TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA reporting TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA audit TO {AppRole};
            """);

        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddReportingModule(appConnectionString)
            .AddAuditModule(appConnectionString)
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Create_then_get_returns_borrador_with_first_version_and_audit()
    {
        var informeId = await CrearInformeAsync(_empresaA, "Informe Q1");

        using (var scope = _provider.CreateScope())
        {
            var detalle = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ObtenerInformeQuery(_empresaA, informeId));
            Assert.True(detalle.IsSuccess);
            Assert.Equal("Borrador", detalle.Value.Estado);
            Assert.Single(detalle.Value.Versiones);

            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            Assert.True(await audit.AuditLogs.AnyAsync(a => a.EventType == "InformeCreado"));
        }
    }

    [Fact]
    public async Task Upload_new_version_keeps_history()
    {
        var informeId = await CrearInformeAsync(_empresaA, "Informe versionado");

        using (var scope = _provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new CargarVersionCommand(_empresaA, informeId, $"{_empresaA:D}/informes/v2.pdf"));
        }

        using (var scope = _provider.CreateScope())
        {
            var detalle = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ObtenerInformeQuery(_empresaA, informeId));
            Assert.Equal(2, detalle.Value.Versiones.Count);
            Assert.Equal(2, detalle.Value.VersionVigenteNumero);
        }
    }

    [Fact]
    public async Task Review_flow_publishes_and_only_published_are_listed_for_client()
    {
        var informeId = await CrearInformeAsync(_empresaA, "Informe a publicar");

        // Antes de publicar, no aparece en publicados.
        using (var scope = _provider.CreateScope())
        {
            var publicados = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ListarPublicadosQuery(_empresaA));
            Assert.Empty(publicados.Value.Items);
        }

        await TransicionarAsync(informeId, EstadoInforme.EnRevision);
        await TransicionarAsync(informeId, EstadoInforme.Aprobado);
        await TransicionarAsync(informeId, EstadoInforme.Publicado);

        using (var scope = _provider.CreateScope())
        {
            var publicados = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ListarPublicadosQuery(_empresaA));
            Assert.Single(publicados.Value.Items);

            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            Assert.True(await audit.AuditLogs.AnyAsync(a => a.EventType == "InformePublicado"));
        }
    }

    [Fact]
    public async Task Generic_transition_to_archived_is_rejected()
    {
        var informeId = await CrearInformeAsync(_empresaA, "Informe");

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new TransicionarEstadoInformeCommand(_empresaA, informeId, EstadoInforme.Archivado));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task Archive_is_logical_deletion_and_audited()
    {
        var informeId = await CrearInformeAsync(_empresaA, "Informe a archivar");

        using (var scope = _provider.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ArchivarInformeCommand(_empresaA, informeId));
            Assert.True(result.IsSuccess);
        }

        using (var scope = _provider.CreateScope())
        {
            var detalle = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ObtenerInformeQuery(_empresaA, informeId));
            Assert.Equal("Archivado", detalle.Value.Estado);

            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            Assert.True(await audit.AuditLogs.AnyAsync(a => a.EventType == "InformeArchivado"));
        }
    }

    [Fact]
    public async Task Reports_are_isolated_per_tenant()
    {
        await CrearInformeAsync(_empresaA, "De A");

        using var scope = _provider.CreateScope();
        var deB = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ListarInformesQuery(_empresaB));

        Assert.Empty(deB.Value.Items);
    }

    private async Task<Guid> CrearInformeAsync(Guid empresaId, string titulo)
    {
        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new CrearInformeCommand(
                empresaId, titulo, TipoEstudio.CalidadAgua,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), null, null, $"{empresaId:D}/informes/v1.pdf"));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private async Task TransicionarAsync(Guid informeId, EstadoInforme nuevoEstado)
    {
        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new TransicionarEstadoInformeCommand(_empresaA, informeId, nuevoEstado));
        Assert.True(result.IsSuccess);
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
}
