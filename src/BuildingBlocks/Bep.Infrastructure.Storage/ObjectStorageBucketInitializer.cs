using Amazon.S3;
using Amazon.S3.Util;
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
    IAmazonS3 s3,
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
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, _options.Bucket))
            {
                await s3.PutBucketAsync(_options.Bucket, cancellationToken);
                logger.LogInformation("Bucket de almacenamiento '{Bucket}' creado.", _options.Bucket);
            }
        }
        catch (AmazonS3Exception ex)
        {
            // No abortar el arranque por el aprovisionamiento del bucket; se registra y continúa.
            logger.LogWarning(ex, "No se pudo asegurar el bucket '{Bucket}' al arrancar.", _options.Bucket);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
