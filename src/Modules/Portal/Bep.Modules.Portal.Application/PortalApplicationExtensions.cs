using Bep.Application.Abstractions.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bep.Modules.Portal.Application;

public static class PortalApplicationExtensions
{
    /// <summary>
    /// Registra los casos de uso del Portal Cliente (M7). No tiene persistencia
    /// propia: agrega las lecturas de Campañas e Informes, que deben registrarse
    /// previamente (AddCampaignModule / AddReportingModule).
    /// </summary>
    public static IServiceCollection AddPortalApplication(this IServiceCollection services)
    {
        var assembly = typeof(PortalApplicationExtensions).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        return services;
    }
}
