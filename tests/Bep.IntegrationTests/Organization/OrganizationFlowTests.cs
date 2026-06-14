using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Organization.Application.Centros.ListarCentros;
using Bep.Modules.Organization.Application.Centros.RegistrarCentro;
using Bep.Modules.Organization.Application.Empresas.ListarEmpresas;
using Bep.Modules.Organization.Application.Empresas.RegistrarEmpresa;
using Bep.Modules.Organization.Infrastructure;
using Bep.Modules.Organization.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Organization;

/// <summary>
/// Ejercita el flujo vertical de M1 (CQRS + validación + RLS) a través del
/// contenedor de DI, igual que en producción: ISender → pipeline de validación →
/// handler → repositorio/EF Core → PostgreSQL con RLS.
/// </summary>
public sealed class OrganizationFlowTests : IAsyncLifetime
{
    private const string AppRole = "bep_app";
    private const string AppPassword = "bep_app_test";
    private const string RutValido = "76000001-9";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("bep")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

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

        await using (var migrationOptions = new OrganizationDbContext(
            new DbContextOptionsBuilder<OrganizationDbContext>().UseNpgsql(adminConnectionString).Options,
            new NullTenantContext()))
        {
            await migrationOptions.Database.MigrateAsync();
        }

        await ExecuteAdminSqlAsync(adminConnectionString, $"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA organization TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA organization TO {AppRole};
            """);

        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddOrganizationModule(appConnectionString)
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Full_flow_register_empresa_and_centro_then_list()
    {
        var empresaId = await RegistrarEmpresaAsync("Salmonera Austral", RutValido);

        Guid centroId;
        using (var scope = _provider.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new RegistrarCentroCommand(empresaId, "Centro Quellón", "QLL-01", -43.12, -73.62, "Los Lagos"));
            Assert.True(result.IsSuccess);
            centroId = result.Value;
        }

        using (var scope = _provider.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ListarCentrosQuery(empresaId));
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items);
            Assert.Equal(centroId, result.Value.Items[0].Id);
        }
    }

    [Fact]
    public async Task Registering_duplicate_rut_returns_conflict()
    {
        await RegistrarEmpresaAsync("Empresa Uno", "77000002-5");

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RegistrarEmpresaCommand("Empresa Dos", "77000002-5", "Acuicultura"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task Invalid_rut_is_rejected_by_validation_pipeline()
    {
        using var scope = _provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new RegistrarEmpresaCommand("Empresa X", "12345678-0", "Acuicultura")));
    }

    [Fact]
    public async Task Centros_are_isolated_per_tenant()
    {
        var empresaA = await RegistrarEmpresaAsync("Salmonera A", "76000001-9");
        var empresaB = await RegistrarEmpresaAsync("Salmonera B", "77000002-5");

        await RegistrarCentroAsync(empresaA, "Centro A", "A-1");
        await RegistrarCentroAsync(empresaB, "Centro B1", "B-1");
        await RegistrarCentroAsync(empresaB, "Centro B2", "B-2");

        using var scope = _provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var soloA = await sender.Send(new ListarCentrosQuery(empresaA));
        Assert.Single(soloA.Value.Items);
        Assert.All(soloA.Value.Items, c => Assert.Equal(empresaA, c.EmpresaId));
    }

    [Fact]
    public async Task List_empresas_is_paged_and_filterable()
    {
        await RegistrarEmpresaAsync("Acuícola Patagonia", "76000001-9");
        await RegistrarEmpresaAsync("Minera Norte", "77000002-5");

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ListarEmpresasQuery(Search: "patagonia", Activa: true, Page: 1, PageSize: 10));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal("Acuícola Patagonia", result.Value.Items[0].RazonSocial);
    }

    private async Task<Guid> RegistrarEmpresaAsync(string razonSocial, string rut)
    {
        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RegistrarEmpresaCommand(razonSocial, rut, "Acuicultura"));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private async Task RegistrarCentroAsync(Guid empresaId, string nombre, string codigo)
    {
        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RegistrarCentroCommand(empresaId, nombre, codigo, -43.0, -73.0, "Los Lagos"));
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
