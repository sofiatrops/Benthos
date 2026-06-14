using Bep.Application.Abstractions.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bep.Modules.Organization.Application;

public static class OrganizationApplicationExtensions
{
    /// <summary>
    /// Registra los casos de uso del módulo (handlers MediatR), sus validadores
    /// y el pipeline de validación (deduplicado entre módulos).
    /// </summary>
    public static IServiceCollection AddOrganizationApplication(this IServiceCollection services)
    {
        var assembly = typeof(OrganizationApplicationExtensions).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        return services;
    }
}
