using Bep.Application.Abstractions.Behaviors;
using Bep.Modules.Laboratory.Application.Parsing;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bep.Modules.Laboratory.Application;

public static class LaboratoryApplicationExtensions
{
    public static IServiceCollection AddLaboratoryApplication(this IServiceCollection services)
    {
        var assembly = typeof(LaboratoryApplicationExtensions).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Estrategias de parseo (Strategy): hoy CSV; futuros formatos/labs se suman aquí.
        services.AddSingleton<IResultadosParser, CsvResultadosParser>();

        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        return services;
    }
}
