using Amazon.Runtime;
using Amazon.S3;
using Bep.Application.Abstractions.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bep.Infrastructure.Storage;

public static class ObjectStorageServiceCollectionExtensions
{
    /// <summary>
    /// Registra el almacenamiento de objetos S3-compatible (ADR-008): cliente S3
    /// (singleton), el adaptador <see cref="IObjectStorage"/> (scoped, para resolver
    /// el tenant de la petición) y, opcionalmente, la creación del bucket al arrancar.
    /// Requiere <c>AddBepTenancy()</c> previo (provee <see cref="Bep.Application.Abstractions.ITenantContext"/>).
    /// </summary>
    public static IServiceCollection AddBepObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ObjectStorageOptions>()
            .Bind(configuration.GetSection(ObjectStorageOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
            var config = new AmazonS3Config
            {
                ForcePathStyle = options.ForcePathStyle,
                AuthenticationRegion = options.Region,
            };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                // MinIO / S3 self-hosted: endpoint explícito en lugar de resolución por región.
                config.ServiceURL = options.ServiceUrl;
                // El SDK firma con HTTPS salvo que se indique HTTP: respetar el esquema del
                // endpoint para que las URLs firmadas apunten al protocolo correcto (MinIO dev).
                config.UseHttp = options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);
            }

            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddScoped<IObjectStorage, S3ObjectStorage>();
        services.AddHostedService<ObjectStorageBucketInitializer>();

        return services;
    }
}
