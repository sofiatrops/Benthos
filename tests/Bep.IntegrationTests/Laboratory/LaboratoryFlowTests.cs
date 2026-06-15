using System.Text;
using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Storage;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Modules.Laboratory.Application.Abstractions;
using Bep.Modules.Laboratory.Application.LoteResultados;
using Bep.Modules.Laboratory.Application.LoteResultados.ImportarResultados;
using Bep.Modules.Laboratory.Application.LoteResultados.Queries;
using Bep.Modules.Laboratory.Domain;
using Bep.Modules.Laboratory.Infrastructure;
using Bep.Modules.Laboratory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Laboratory;

/// <summary>
/// Flujo del módulo de Laboratorios (M4): ingesta de un CSV (referenciado por
/// object key, ADR-008), validación profesional que enciende los KPIs y aislamiento
/// estricto por tenant (RLS). El contenido del archivo lo entrega un almacén de
/// prueba; el round-trip real contra MinIO se cubre en ObjectStorageTests.
/// </summary>
public sealed class LaboratoryFlowTests : IAsyncLifetime
{
    private const string AppRole = "bep_app";
    private const string AppPassword = "bep_app_test";

    private const string Csv = """
        codigo_muestra,parametro,valor,unidad,metodo
        MTR-20260601-AAAAA00001,Oxígeno disuelto,8.4,mg/L,SM 4500-O
        MTR-20260601-AAAAA00001,pH,7.9,pH
        MTR-20260601-BBBBB00002,Temperatura,no-numero,C
        """;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("bep").WithUsername("postgres").WithPassword("postgres").Build();

    private readonly Guid _empresaA = Guid.NewGuid();
    private readonly Guid _empresaB = Guid.NewGuid();
    private readonly Guid _campana = Guid.NewGuid();

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

        await MigrarAsync(adminConnectionString);
        await ExecuteAdminSqlAsync(adminConnectionString, $"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA laboratory TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA laboratory TO {AppRole};
            """);

        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddScoped<ICurrentUser>(_ => new StubStaff())
            .AddScoped<IObjectStorage>(_ => new CsvStubStorage(Csv))
            .AddLaboratoryModule(appConnectionString)
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Importar_crea_lote_con_resultados_validos_y_reporta_errores()
    {
        var result = await ImportarAsync(_empresaA);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Importados);
        Assert.Single(result.Value.Errores); // fila con valor no numérico

        using var scope = _provider.CreateScope();
        var lote = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ObtenerLoteQuery(_empresaA, result.Value.LoteId));

        Assert.True(lote.IsSuccess);
        Assert.Equal(nameof(EstadoLote.Recibido), lote.Value.Estado);
        Assert.Equal(2, lote.Value.Resultados.Count);
    }

    [Fact]
    public async Task Validar_lote_enciende_los_kpis()
    {
        var importado = await ImportarAsync(_empresaA);

        // Antes de validar, los KPIs están vacíos (solo cuentan lotes validados).
        Assert.Empty(await KpisAsync(_empresaA));

        using (var scope = _provider.CreateScope())
        {
            var r = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ValidarLoteCommand(_empresaA, importado.Value.LoteId));
            Assert.True(r.IsSuccess);
        }

        var kpis = await KpisAsync(_empresaA);
        Assert.Equal(1, ValorKpi(kpis, "Lotes validados"));
        Assert.Equal(2, ValorKpi(kpis, "Parámetros analizados"));
        Assert.Equal(1, ValorKpi(kpis, "Muestras con resultado"));
    }

    [Fact]
    public async Task Cliente_no_ve_lotes_de_otro_tenant_RLS()
    {
        var loteB = await ImportarAsync(_empresaB);

        using var scope = _provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ObtenerLoteQuery(_empresaA, loteB.Value.LoteId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    private async Task<Result<ImportarResultadosResult>> ImportarAsync(Guid empresaId)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ImportarResultadosCommand(empresaId, _campana, "Laboratorio Austral", $"{empresaId:D}/laboratorio/resultados.csv"));
    }

    private async Task<IReadOnlyList<LaboratorioKpi>> KpisAsync(Guid empresaId)
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(empresaId);
        return await scope.ServiceProvider.GetRequiredService<ILaboratoryReadService>().GetKpisAsync(empresaId);
    }

    private static double ValorKpi(IReadOnlyList<LaboratorioKpi> kpis, string nombre)
        => kpis.Single(k => k.Nombre == nombre).Valor;

    private static async Task MigrarAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<LaboratoryDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new LaboratoryDbContext(options, new NullTenantContext());
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

    private sealed class StubStaff : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? SubjectId => "coordinador-1";
        public PrincipalType PrincipalType => PrincipalType.BenthosStaff;
        public Guid? TenantId => null;
        public IReadOnlyCollection<string> Roles => ["coordinador"];
    }

    /// <summary>Almacén de prueba: entrega el CSV configurado y no toca S3.</summary>
    private sealed class CsvStubStorage(string csv) : IObjectStorage
    {
        public Task<Result<UploadTicket>> CrearTicketSubidaAsync(SolicitudSubida solicitud, CancellationToken ct)
            => Task.FromResult(Result.Success(new UploadTicket(
                "k", new Uri("https://storage.local/u"), solicitud.ContentType, DateTimeOffset.UtcNow.AddMinutes(10))));

        public Task<Result<Uri>> CrearUrlDescargaAsync(string objectKey, CancellationToken ct)
            => Task.FromResult(Result.Success(new Uri($"https://storage.local/{objectKey}")));

        public Task<Result<Stream>> AbrirLecturaAsync(string objectKey, CancellationToken ct)
            => Task.FromResult(Result.Success<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(csv))));
    }
}
