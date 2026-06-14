using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Audit.Infrastructure;
using Bep.Modules.Audit.Infrastructure.Persistence;
using Bep.Modules.Campaign.Application.Campanias.AsignarResponsables;
using Bep.Modules.Campaign.Application.Campanias.CrearCampana;
using Bep.Modules.Campaign.Application.Campanias.ListarCampanas;
using Bep.Modules.Campaign.Application.Campanias.ObtenerCampana;
using Bep.Modules.Campaign.Application.Campanias.TransicionarEstado;
using Bep.Modules.Campaign.Domain;
using Bep.Modules.Campaign.Infrastructure;
using Bep.Modules.Campaign.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Campaign;

/// <summary>
/// Flujo vertical de M2: crear campaña, transicionar estado (con auditoría),
/// filtrar el listado y comprobar el aislamiento por tenant (RLS).
/// </summary>
public sealed class CampaignFlowTests : IAsyncLifetime
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

        await using (var campaign = new CampaignDbContext(
            new DbContextOptionsBuilder<CampaignDbContext>().UseNpgsql(adminConnectionString).Options,
            new NullTenantContext()))
        {
            await campaign.Database.MigrateAsync();
        }

        await using (var audit = new AuditDbContext(
            new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(adminConnectionString).Options))
        {
            await audit.Database.MigrateAsync();
        }

        await ExecuteAdminSqlAsync(adminConnectionString, $"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA campaign, audit TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA campaign TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA audit TO {AppRole};
            """);

        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddCampaignModule(appConnectionString)
            .AddAuditModule(appConnectionString)
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Create_then_get_campaign_returns_planificada()
    {
        var centroId = Guid.NewGuid();
        var campanaId = await CrearCampanaAsync(_empresaA, "Campaña A", [centroId]);

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ObtenerCampanaQuery(_empresaA, campanaId));

        Assert.True(result.IsSuccess);
        Assert.Equal("Planificada", result.Value.Estado);
        Assert.Contains(centroId, result.Value.CentroIds);
    }

    [Fact]
    public async Task Transition_changes_state_and_is_audited()
    {
        var campanaId = await CrearCampanaAsync(_empresaA, "Campaña transición", [Guid.NewGuid()]);

        using (var scope = _provider.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new TransicionarEstadoCommand(_empresaA, campanaId, EstadoCampania.EnCurso));
            Assert.True(result.IsSuccess);
        }

        using (var scope = _provider.CreateScope())
        {
            var campania = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ObtenerCampanaQuery(_empresaA, campanaId));
            Assert.Equal("EnCurso", campania.Value.Estado);

            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            Assert.True(await audit.AuditLogs.AnyAsync(a => a.EventType == "EstadoCampanaCambiado"));
        }
    }

    [Fact]
    public async Task Invalid_transition_returns_conflict()
    {
        var campanaId = await CrearCampanaAsync(_empresaA, "Campaña inválida", [Guid.NewGuid()]);

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new TransicionarEstadoCommand(_empresaA, campanaId, EstadoCampania.Cerrada));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task List_filters_by_estado_and_centro()
    {
        var centroId = Guid.NewGuid();
        await CrearCampanaAsync(_empresaA, "Con centro", [centroId]);
        await CrearCampanaAsync(_empresaA, "Otro centro", [Guid.NewGuid()]);

        using var scope = _provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var porCentro = await sender.Send(new ListarCampanasQuery(_empresaA, CentroId: centroId));
        Assert.Single(porCentro.Value.Items);

        var planificadas = await sender.Send(new ListarCampanasQuery(_empresaA, Estado: EstadoCampania.Planificada));
        Assert.Equal(2, planificadas.Value.TotalCount);
    }

    [Fact]
    public async Task List_filters_by_responsable()
    {
        var campanaId = await CrearCampanaAsync(_empresaA, "Con responsable", [Guid.NewGuid()]);
        using (var scope = _provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new AsignarResponsablesCommand(_empresaA, campanaId, [new ResponsableInput("sub-coord", "coordinador")]));
        }

        using var queryScope = _provider.CreateScope();
        var result = await queryScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ListarCampanasQuery(_empresaA, ResponsableSubjectId: "sub-coord"));

        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task Campaigns_are_isolated_per_tenant()
    {
        await CrearCampanaAsync(_empresaA, "De A", [Guid.NewGuid()]);

        using var scope = _provider.CreateScope();
        var deB = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ListarCampanasQuery(_empresaB));

        Assert.Empty(deB.Value.Items);
    }

    private async Task<Guid> CrearCampanaAsync(Guid empresaId, string nombre, IReadOnlyList<Guid> centroIds)
    {
        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new CrearCampanaCommand(
                empresaId, nombre, "desc", TipoCampania.Mixta,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), centroIds));
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
