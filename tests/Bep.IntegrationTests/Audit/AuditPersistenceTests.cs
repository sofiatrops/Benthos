using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Audit.Infrastructure;
using Bep.Modules.Audit.Infrastructure.Persistence;
using Bep.Modules.Organization.Application.Empresas.RegistrarEmpresa;
using Bep.Modules.Organization.Infrastructure;
using Bep.Modules.Organization.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Audit;

/// <summary>
/// Verifica que los eventos de dominio se persisten como auditoría (M8, RF-08-003)
/// y que los registros son inmutables a nivel de base de datos (RF-08-007): el
/// trigger bloquea UPDATE/DELETE incluso cuando el rol tiene esos privilegios.
/// </summary>
public sealed class AuditPersistenceTests : IAsyncLifetime
{
    private const string AppRole = "bep_app";
    private const string AppPassword = "bep_app_test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("bep")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private ServiceProvider _provider = null!;
    private string _appConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var adminConnectionString = _postgres.GetConnectionString();
        _appConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Username = AppRole,
            Password = AppPassword,
        }.ConnectionString;

        await using (var organization = new OrganizationDbContext(
            new DbContextOptionsBuilder<OrganizationDbContext>().UseNpgsql(adminConnectionString).Options,
            new NullTenantContext()))
        {
            await organization.Database.MigrateAsync();
        }

        await using (var audit = new AuditDbContext(
            new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(adminConnectionString).Options))
        {
            await audit.Database.MigrateAsync();
        }

        await ExecuteAdminSqlAsync(adminConnectionString, $"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA organization, audit TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA organization TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA audit TO {AppRole};
            """);

        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddOrganizationModule(_appConnectionString)
            .AddAuditModule(_appConnectionString)
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Domain_event_is_persisted_as_audit_log()
    {
        using (var scope = _provider.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new RegistrarEmpresaCommand("Salmonera Auditada", "76000001-9", "Acuicultura"));
            Assert.True(result.IsSuccess);
        }

        using (var scope = _provider.CreateScope())
        {
            var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            var log = await audit.AuditLogs.SingleOrDefaultAsync(a => a.EventType == "EmpresaRegistrada");

            Assert.NotNull(log);
            Assert.Contains("Salmonera Auditada", log!.PayloadJson);
        }
    }

    [Fact]
    public async Task Audit_log_cannot_be_updated_or_deleted()
    {
        using (var scope = _provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new RegistrarEmpresaCommand("Empresa Inmutable", "77000002-5", "Acuicultura"));
        }

        await using var connection = new NpgsqlConnection(_appConnectionString);
        await connection.OpenAsync();

        await using var update = connection.CreateCommand();
        update.CommandText = "UPDATE audit.audit_log SET \"ActorSubjectId\" = 'hacker';";
        var updateException = await Assert.ThrowsAsync<PostgresException>(() => update.ExecuteNonQueryAsync());
        Assert.Contains("inmutables", updateException.MessageText);

        await using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM audit.audit_log;";
        await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());
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
