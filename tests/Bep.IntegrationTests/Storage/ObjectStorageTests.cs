using System.Net.Http.Headers;
using System.Text;
using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Storage;
using Bep.Infrastructure.Common.DependencyInjection;
using Bep.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Minio;

namespace Bep.IntegrationTests.Storage;

/// <summary>
/// Integración real del almacenamiento de objetos contra MinIO (ADR-008). Verifica
/// el ciclo de URL firmada subida→descarga, el aislamiento por prefijo de tenant
/// (un tenant no puede firmar descargas de otro = cierre de IDOR) y las validaciones
/// de tipo de contenido y tamaño previas a la subida.
/// </summary>
public sealed class ObjectStorageTests : IAsyncLifetime
{
    private readonly MinioContainer _minio = new MinioBuilder("minio/minio:latest").Build();

    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        await _minio.StartAsync();

        // MinIO sirve por HTTP en el puerto 9000; endpoint explícito con esquema para
        // que las URLs firmadas (y el cliente S3) usen HTTP y no intenten TLS.
        var endpoint = $"http://{_minio.Hostname}:{_minio.GetMappedPublicPort(9000)}";

        var settings = new Dictionary<string, string?>
        {
            ["ObjectStorage:ServiceUrl"] = endpoint,
            ["ObjectStorage:Bucket"] = "bep-test",
            ["ObjectStorage:AccessKey"] = _minio.GetAccessKey(),
            ["ObjectStorage:SecretKey"] = _minio.GetSecretKey(),
            ["ObjectStorage:ForcePathStyle"] = "true",
            ["ObjectStorage:CrearBucketSiNoExiste"] = "true",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        _provider = new ServiceCollection()
            .AddLogging()
            .AddBepTenancy()
            .AddBepObjectStorage(configuration)
            .BuildServiceProvider();

        // En un ServiceProvider plano los IHostedService no arrancan solos: ejecutamos
        // el inicializador para crear el bucket de pruebas.
        foreach (var hosted in _provider.GetServices<IHostedService>())
        {
            await hosted.StartAsync(CancellationToken.None);
        }
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _minio.DisposeAsync();
    }

    [Fact]
    public async Task Signed_upload_then_download_round_trips_the_file()
    {
        var contenido = Encoding.UTF8.GetBytes("%PDF-1.7 contenido de prueba");
        using var http = new HttpClient();

        using var scope = NuevoScopeParaTenant(_tenantA, out var storage);

        var ticket = await storage.CrearTicketSubidaAsync(
            new SolicitudSubida(CategoriasObjeto.InformesPdf, "informe.pdf", "application/pdf", contenido.Length),
            CancellationToken.None);
        Assert.True(ticket.IsSuccess);
        Assert.StartsWith($"{_tenantA:D}/", ticket.Value.ObjectKey, StringComparison.Ordinal);

        // PUT directo al almacén con la URL firmada y el Content-Type declarado.
        using var carga = new ByteArrayContent(contenido);
        carga.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var subida = await http.PutAsync(ticket.Value.UrlSubida, carga);
        subida.EnsureSuccessStatusCode();

        var descarga = await storage.CrearUrlDescargaAsync(ticket.Value.ObjectKey, CancellationToken.None);
        Assert.True(descarga.IsSuccess);

        var bytes = await http.GetByteArrayAsync(descarga.Value);
        Assert.Equal(contenido, bytes);
    }

    [Fact]
    public async Task Download_url_for_another_tenant_key_is_forbidden()
    {
        // Clave bajo el prefijo del tenant B.
        var claveDeB = ObjectKeys.Construir(_tenantB, CategoriasObjeto.InformesPdf, "confidencial.pdf");

        // El tenant A intenta firmar su descarga: rechazado por el adaptador.
        using var scope = NuevoScopeParaTenant(_tenantA, out var storage);
        var result = await storage.CrearUrlDescargaAsync(claveDeB, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task Upload_ticket_rejects_disallowed_content_type()
    {
        using var scope = NuevoScopeParaTenant(_tenantA, out var storage);
        var result = await storage.CrearTicketSubidaAsync(
            new SolicitudSubida(CategoriasObjeto.InformesPdf, "malware.exe", "application/x-msdownload", 1024),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal("storage.content_type_no_permitido", result.Error.Code);
    }

    [Fact]
    public async Task Upload_ticket_rejects_oversized_file()
    {
        using var scope = NuevoScopeParaTenant(_tenantA, out var storage);
        var result = await storage.CrearTicketSubidaAsync(
            new SolicitudSubida(CategoriasObjeto.InformesPdf, "enorme.pdf", "application/pdf", 100L * 1024 * 1024 * 1024),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("storage.tamano_excedido", result.Error!.Code);
    }

    private IServiceScope NuevoScopeParaTenant(Guid tenantId, out IObjectStorage storage)
    {
        var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId);
        storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        return scope;
    }
}
