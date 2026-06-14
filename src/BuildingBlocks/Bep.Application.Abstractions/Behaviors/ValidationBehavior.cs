using FluentValidation;
using MediatR;

namespace Bep.Application.Abstractions.Behaviors;

/// <summary>
/// Comportamiento transversal que valida la petición con FluentValidation antes
/// del handler (Decorator sobre el pipeline, SRS 2.7.4). Las violaciones se lanzan
/// como <see cref="ValidationException"/> y se mapean a HTTP 400 en la API.
/// Compartido por todos los módulos (DRY).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
