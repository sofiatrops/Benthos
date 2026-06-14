using Bep.Application.Abstractions.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bep.Modules.Campaign.Application;

public static class CampaignApplicationExtensions
{
    public static IServiceCollection AddCampaignApplication(this IServiceCollection services)
    {
        var assembly = typeof(CampaignApplicationExtensions).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Registro deduplicado: el pipeline de validación es único aunque varios
        // módulos lo declaren (TryAddEnumerable evita ejecutarlo varias veces).
        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        return services;
    }
}
