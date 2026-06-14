using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Bep.Api.Http;

/// <summary>
/// Traduce las <see cref="ValidationException"/> del pipeline a una respuesta
/// HTTP 400 con el detalle de los errores por campo (RFC 7807).
/// </summary>
public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = Results.ValidationProblem(errors, title: "Solicitud inválida");
        await problem.ExecuteAsync(httpContext);
        return true;
    }
}
