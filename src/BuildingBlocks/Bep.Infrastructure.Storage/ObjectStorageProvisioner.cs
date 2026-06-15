using Amazon.S3;
using Amazon.S3.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bep.Infrastructure.Storage;

/// <summary>
/// Aprovisionamiento del bucket de objetos. Reutilizable tanto por el inicializador
/// de arranque como por flujos que necesitan el bucket antes de que arranquen los
/// <c>IHostedService</c> (p. ej. siembras de datos en Development).
/// </summary>
public static class ObjectStorageProvisioner
{
    /// <summary>Crea el bucket configurado si no existe. Idempotente.</summary>
    public static async Task EnsureBucketAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var s3 = services.GetRequiredService<IAmazonS3>();
        var bucket = services.GetRequiredService<IOptions<ObjectStorageOptions>>().Value.Bucket;

        if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, bucket))
        {
            await s3.PutBucketAsync(bucket, cancellationToken);
        }
    }
}
