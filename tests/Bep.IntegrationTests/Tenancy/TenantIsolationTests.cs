using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Organization.Domain;
using Bep.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bep.IntegrationTests.Tenancy;

/// <summary>
/// Verifica el aislamiento multi-tenant a nivel de base de datos mediante
/// Row-Level Security (ADR-004, RNF-SEG-008). Las consultas se ejecutan como un
/// rol de aplicación SIN privilegios de superusuario (igual que en producción),
/// de modo que la RLS no pueda evadirse.
///
/// <para>
/// Clave de la prueba: se usa <c>IgnoreQueryFilters()</c> para desactivar el
/// filtro de la capa de aplicación. Si aun así no se filtran datos de otro
/// tenant, es porque la RLS de PostgreSQL está haciendo cumplir el aislamiento.
/// </para>
/// </summary>
public sealed class TenantIsolationTests : IAsyncLifetime
{
    private const string AppRole = "bep_app";
    private const string AppPassword = "bep_app_test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("bep")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private string _adminConnectionString = string.Empty;
    private string _appConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _adminConnectionString = _postgres.GetConnectionString();

        var adminBuilder = new NpgsqlConnectionStringBuilder(_adminConnectionString);
        _appConnectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Username = AppRole,
            Password = AppPassword,
        }.ConnectionString;

        // 1) Esquema + tablas + políticas RLS (vía migraciones, como superusuario).
        await using (var migrationContext = CreateContext(_adminConnectionString, new FixedTenantContext(null), withInterceptor: false))
        {
            await migrationContext.Database.MigrateAsync();
        }

        // 2) Rol de aplicación no-superusuario + permisos.
        await ExecuteAdminSqlAsync($"""
            CREATE ROLE {AppRole} LOGIN PASSWORD '{AppPassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT USAGE ON SCHEMA organization TO {AppRole};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA organization TO {AppRole};
            """);

        // 3) Datos semilla de dos tenants (como superusuario, sin restricción RLS).
        await SeedTenantAsync(_tenantA, "Salmonera A", "76000001", centros: 2);
        await SeedTenantAsync(_tenantB, "Salmonera B", "77000002", centros: 3);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Query_with_tenant_A_returns_only_tenant_A_rows_even_ignoring_app_filter()
    {
        await using var context = CreateContext(_appConnectionString, new FixedTenantContext(_tenantA));

        // IgnoreQueryFilters desactiva el filtro de aplicación: lo que quede es RLS.
        var centros = await context.Set<Centro>().IgnoreQueryFilters().ToListAsync();

        Assert.NotEmpty(centros);
        Assert.All(centros, c => Assert.Equal(_tenantA, c.TenantId));
        Assert.DoesNotContain(centros, c => c.TenantId == _tenantB);
    }

    [Fact]
    public async Task Query_with_tenant_B_cannot_see_tenant_A_rows()
    {
        await using var context = CreateContext(_appConnectionString, new FixedTenantContext(_tenantB));

        var centros = await context.Set<Centro>().IgnoreQueryFilters().ToListAsync();

        Assert.NotEmpty(centros);
        Assert.All(centros, c => Assert.Equal(_tenantB, c.TenantId));
    }

    [Fact]
    public async Task Query_without_resolved_tenant_returns_no_rows_deny_by_default()
    {
        await using var context = CreateContext(_appConnectionString, new FixedTenantContext(null));

        var centros = await context.Set<Centro>().IgnoreQueryFilters().ToListAsync();

        Assert.Empty(centros);
    }

    [Fact]
    public async Task Insert_into_another_tenant_is_rejected_by_rls_with_check()
    {
        await using var context = CreateContext(_appConnectionString, new FixedTenantContext(_tenantA));

        // Contexto = tenant A, pero la fila pertenece al tenant B: WITH CHECK lo bloquea.
        var centroDeOtroTenant = Centro.Crear(
            _tenantB, "Centro intruso", "INTRUSO-1", CoordenadasGps.Create(-41.0, -73.0), "Los Lagos");
        context.Add(centroDeOtroTenant);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private OrganizationDbContext CreateContext(string connectionString, ITenantContext tenantContext, bool withInterceptor = true)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(connectionString);

        if (withInterceptor)
        {
            optionsBuilder.AddInterceptors(new TenantConnectionInterceptor(tenantContext));
        }

        return new OrganizationDbContext(optionsBuilder.Options, tenantContext);
    }

    private async Task SeedTenantAsync(Guid tenantId, string razonSocial, string rutBody, int centros)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        await using (var insertEmpresa = connection.CreateCommand())
        {
            insertEmpresa.CommandText = """
                INSERT INTO organization.empresa ("Id", "RazonSocial", rut, "Rubro", "Activa", "CreadaUtc")
                VALUES (@id, @razon, @rut, @rubro, true, now());
                """;
            insertEmpresa.Parameters.AddWithValue("id", tenantId);
            insertEmpresa.Parameters.AddWithValue("razon", razonSocial);
            insertEmpresa.Parameters.AddWithValue("rut", $"{rutBody}-0");
            insertEmpresa.Parameters.AddWithValue("rubro", "Acuicultura");
            await insertEmpresa.ExecuteNonQueryAsync();
        }

        for (var i = 0; i < centros; i++)
        {
            await using var insertCentro = connection.CreateCommand();
            insertCentro.CommandText = """
                INSERT INTO organization.centro ("Id", tenant_id, "Nombre", "CodigoInterno", latitud, longitud, "Region", "Activo")
                VALUES (@id, @tenant, @nombre, @codigo, @lat, @lon, @region, true);
                """;
            insertCentro.Parameters.AddWithValue("id", Guid.NewGuid());
            insertCentro.Parameters.AddWithValue("tenant", tenantId);
            insertCentro.Parameters.AddWithValue("nombre", $"Centro {i + 1}");
            insertCentro.Parameters.AddWithValue("codigo", $"C-{i + 1}");
            insertCentro.Parameters.AddWithValue("lat", -41.0 - i);
            insertCentro.Parameters.AddWithValue("lon", -73.0 - i);
            insertCentro.Parameters.AddWithValue("region", "Los Lagos");
            await insertCentro.ExecuteNonQueryAsync();
        }
    }

    private async Task ExecuteAdminSqlAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Contexto de tenant fijo, para controlar el tenant efectivo en cada prueba.</summary>
    private sealed class FixedTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? TenantId { get; private set; } = tenantId;

        public bool HasTenant => TenantId.HasValue;

        public void SetTenant(Guid value) => TenantId = value;
    }
}
