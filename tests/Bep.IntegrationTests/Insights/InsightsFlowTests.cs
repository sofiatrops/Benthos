using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Insights.Application.Abstractions;
using Bep.Modules.Insights.Application.Analisis;
using Bep.Modules.Insights.Application.Analisis.GenerarAnalisis;
using Bep.Modules.Insights.Application.Analisis.Queries;
using Bep.Modules.Insights.Domain;
using Bep.Modules.Insights.Infrastructure;
using Bep.Modules.Insights.Infrastructure.Persistence;
using Bep.Modules.Laboratory.Application.Abstractions;
using Bep.Modules.Laboratory.Application.LoteResultados;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Insights;

/// <summary>
/// Flujo del módulo de IA Ambiental (M6): generación de un borrador a partir de
/// resultados de laboratorio validados (proveedor determinista), validación
/// profesional (humano en el bucle) y aislamiento por tenant (RLS). Los resultados
/// de M4 los entrega un read service de prueba.
/// </summary>
public sealed class InsightsFlowTests : IAsyncLifetime
{
    private const string AppRole = "bep_app";
    private const string AppPassword = "bep_app_test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("bep").WithUsername("postgres").WithPassword("postgres").Build();

    private readonly Guid _empresaA = Guid.NewGuid();
    private readonly Guid _empresaB = Guid.NewGuid();
    private readonly Guid _campanaConDatos = Guid.NewGuid();

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

        var options = new DbContextOptionsBuilder<InsightsDbContext>().UseNpgsql(adminConnectionString).Options;
        await using (var context = new InsightsDbContext(options, new NullTenantContext()))
        {
            await context.Database.MigrateAsync();
        }

        await ExecuteAdminSqlAsync(adminConnectionString, $"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA insights TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA insights TO {AppRole};
            """);

        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddScoped<ICurrentUser>(_ => new StubStaff())
            .AddScoped<ILaboratoryReadService>(_ => new StubLaboratory(_campanaConDatos))
            .AddInsightsModule(appConnectionString, new ConfigurationBuilder().Build())
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Generar_crea_borrador_con_resumen_y_hallazgos()
    {
        var generado = await GenerarAsync(_empresaA, _campanaConDatos);
        Assert.True(generado.IsSuccess);

        using var scope = _provider.CreateScope();
        var analisis = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ObtenerAnalisisQuery(_empresaA, generado.Value));

        Assert.True(analisis.IsSuccess);
        Assert.Equal(nameof(EstadoAnalisis.Borrador), analisis.Value.Estado);
        Assert.False(string.IsNullOrWhiteSpace(analisis.Value.Resumen));
        Assert.NotEmpty(analisis.Value.Hallazgos);
        Assert.Equal("deterministic-v1", analisis.Value.Modelo);
    }

    [Fact]
    public async Task Generar_sin_resultados_validados_falla()
    {
        var result = await GenerarAsync(_empresaA, Guid.NewGuid()); // campaña sin datos

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Validar_publica_el_analisis_como_ultimo_validado()
    {
        var generado = await GenerarAsync(_empresaA, _campanaConDatos);

        using (var scope = _provider.CreateScope())
        {
            var r = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ValidarAnalisisCommand(_empresaA, generado.Value));
            Assert.True(r.IsSuccess);
        }

        using var verify = _provider.CreateScope();
        verify.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(_empresaA);
        var ultimo = await verify.ServiceProvider.GetRequiredService<IInsightsReadService>()
            .GetUltimoValidadoAsync(_empresaA);
        Assert.NotNull(ultimo);
        Assert.Equal(nameof(EstadoAnalisis.Validado), ultimo!.Estado);
    }

    [Fact]
    public async Task No_se_ve_analisis_de_otro_tenant_RLS()
    {
        var generadoB = await GenerarAsync(_empresaB, _campanaConDatos);

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ObtenerAnalisisQuery(_empresaA, generadoB.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    private async Task<Result<Guid>> GenerarAsync(Guid empresaId, Guid campanaId)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new GenerarAnalisisCommand(empresaId, campanaId));
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

    private sealed class StubStaff : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? SubjectId => "revisor-1";
        public PrincipalType PrincipalType => PrincipalType.BenthosStaff;
        public Guid? TenantId => null;
        public IReadOnlyCollection<string> Roles => ["revisor"];
    }

    /// <summary>Read service de M4 de prueba: entrega resultados solo para la campaña con datos.</summary>
    private sealed class StubLaboratory(Guid campanaConDatos) : ILaboratoryReadService
    {
        public Task<LoteResultadosDto?> GetLoteAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<LoteResultadosDto?>(null);

        public Task<Bep.Application.Abstractions.PagedResult<LoteResumenDto>> ListLotesAsync(
            Guid empresaId, Guid? campanaId, Bep.Modules.Laboratory.Domain.EstadoLote? estado,
            Bep.Application.Abstractions.PageRequest page, CancellationToken ct = default)
            => Task.FromResult(new Bep.Application.Abstractions.PagedResult<LoteResumenDto>([], 1, 20, 0));

        public Task<IReadOnlyList<LaboratorioKpi>> GetKpisAsync(Guid empresaId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<LaboratorioKpi>>([]);

        public Task<IReadOnlyList<ResultadoParametroDto>> GetResultadosValidadosPorCampanaAsync(
            Guid empresaId, Guid campanaId, CancellationToken ct = default)
        {
            if (campanaId != campanaConDatos)
            {
                return Task.FromResult<IReadOnlyList<ResultadoParametroDto>>([]);
            }

            IReadOnlyList<ResultadoParametroDto> datos =
            [
                new("MTR-A", "pH", 7.8, "pH", null),
                new("MTR-A", "pH", 8.0, "pH", null),
                new("MTR-B", "Oxígeno disuelto", 7.0, "mg/L", "SM 4500-O"),
                new("MTR-B", "Oxígeno disuelto", 9.0, "mg/L", "SM 4500-O"),
            ];
            return Task.FromResult(datos);
        }
    }
}
