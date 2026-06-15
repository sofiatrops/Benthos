using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bep.Infrastructure.Storage;

/// <summary>
/// Crea el bucket configurado al arrancar si <see cref="ObjectStorageOptions.CrearBucketSiNoExiste"/>
/// está activo. Conveniencia de desarrollo (MinIO recién levantado); en producción el
/// bucket se aprovisiona por infraestructura y esta opción permanece desactivada.
/// </summary>
internal sealed class ObjectStorageBucketInitializer(
    IServiceProvider services,
    IOptions<ObjectStorageOptions> options,
    ILogger<ObjectStorageBucketInitializer> logger) : IHostedService
{
    private readonly ObjectStorageOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.CrearBucketSiNoExiste)
        {
            return;
        }

        try
        {
            await ObjectStorageProvisioner.EnsureBucketAsync(services, cancellationToken);
            logger.LogInformation("Bucket de almacenamiento '{Bucket}' asegurado.", _options.Bucket);
        }
#pragma warning disable CA1031 // El aprovisionamiento del bucket nunca debe abortar el arranque (almacén caído, red, etc.).
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // No abortar el arranque por el aprovisionamiento del bucket; se registra y continúa.
            logger.LogWarning(ex, "No se pudo asegurar el bucket '{Bucket}' al arrancar.", _options.Bucket);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
