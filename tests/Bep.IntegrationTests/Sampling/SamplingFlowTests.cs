using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Audit.Infrastructure;
using Bep.Modules.Audit.Infrastructure.Persistence;
using Bep.Modules.Sampling.Application.Muestras.Custodia;
using Bep.Modules.Sampling.Application.Muestras.Queries;
using Bep.Modules.Sampling.Application.Muestras.RegistrarMuestra;
using Bep.Modules.Sampling.Domain;
using Bep.Modules.Sampling.Infrastructure;
using Bep.Modules.Sampling.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Sampling;

/// <summary>
/// Flujo vertical de M3: registrar muestra (código + QR + GPS), consultar por QR
/// (RF-03-008), cadena de custodia (RF-03-007), trazabilidad auditada y aislamiento
/// por tenant (RLS).
/// </summary>
public sealed class SamplingFlowTests : IAsyncLifetime
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
    private readonly Guid _campana = Guid.NewGuid();
    private readonly Guid _centro = Guid.NewGuid();

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

        await using (var sampling = new SamplingDbContext(
            new DbContextOptionsBuilder<SamplingDbContext>().UseNpgsql(adminConnectionString).Options,
            new NullTenantContext()))
        {
            await sampling.Database.MigrateAsync();
        }

        await using (var audit = new AuditDbContext(
            new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(adminConnectionString).Options))
        {
            await audit.Database.MigrateAsync();
        }

        await ExecuteAdminSqlAsync(adminConnectionString, $"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA sampling, audit TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA sampling TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA audit TO {AppRole};
            """);

        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddSamplingModule(appConnectionString)
            .AddAuditModule(appConnectionString)
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Register_sample_then_query_by_qr_returns_it()
    {
        var muestraId = await RegistrarMuestraAsync(_empresaA);

        string codigoQr;
        using (var scope = _provider.CreateScope())
        {
            var detalle = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ObtenerMuestraQuery(_empresaA, muestraId));
            Assert.True(detalle.IsSuccess);
            Assert.Equal("Registrada", detalle.Value.Estado);
            Assert.StartsWith("MTR-", detalle.Value.CodigoUnico);
            Assert.Contains(detalle.Value.Eventos, e => e.Tipo == "Registro");
            codigoQr = detalle.Value.CodigoQr;
        }

        using (var scope = _provider.CreateScope())
        {
            var porQr = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ConsultarPorQrQuery(_empresaA, codigoQr));
            Assert.True(porQr.IsSuccess);
            Assert.Equal(muestraId, porQr.Value.Id);
        }
    }

    [Fact]
    public async Task Registration_is_audited()
    {
        await RegistrarMuestraAsync(_empresaA);

        using var scope = _provider.CreateScope();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        Assert.True(await audit.AuditLogs.AnyAsync(a => a.EventType == "MuestraRegistrada"));
    }

    [Fact]
    public async Task Custody_transfer_and_accept_moves_to_lab()
    {
        var muestraId = await RegistrarMuestraAsync(_empresaA);

        using (var scope = _provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new TransferirCustodiaCommand(_empresaA, muestraId, "laboratorio-quellon"));
        }

        using (var scope = _provider.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new AceptarCustodiaCommand(_empresaA, muestraId));
            Assert.True(result.IsSuccess);
        }

        using (var scope = _provider.CreateScope())
        {
            var detalle = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ObtenerMuestraQuery(_empresaA, muestraId));
            Assert.Equal("RecibidaLaboratorio", detalle.Value.Estado);
            Assert.Single(detalle.Value.Custodias);
            Assert.True(detalle.Value.Custodias[0].Aceptada);
        }
    }

    [Fact]
    public async Task List_samples_of_campaign()
    {
        await RegistrarMuestraAsync(_empresaA);
        await RegistrarMuestraAsync(_empresaA);

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ListarMuestrasQuery(_empresaA, _campana));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task Samples_are_isolated_per_tenant()
    {
        await RegistrarMuestraAsync(_empresaA);

        using var scope = _provider.CreateScope();
        var deB = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ListarMuestrasQuery(_empresaB, _campana));

        Assert.Empty(deB.Value.Items);
    }

    private async Task<Guid> RegistrarMuestraAsync(Guid empresaId)
    {
        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RegistrarMuestraCommand(
                empresaId, _campana, _centro, TipoMuestra.Agua,
                ["oxigeno_disuelto", "ph"], -41.5, -73.0, 5));
        Assert.True(result.IsSuccess);
        return result.Value;
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
