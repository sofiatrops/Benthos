using Bep.Application.Abstractions;

namespace Bep.Api.Http;

/// <summary>
/// Traduce un <see cref="Result"/> de la capa de aplicación a una respuesta HTTP,
/// mapeando el tipo de error al código de estado (RFC 7807 ProblemDetails).
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
        => result.IsSuccess ? Results.NoContent() : Problem(result.Error!);

    public static IResult ToHttpResult<T>(this Result<T> result)
        => result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory)
        => result.IsSuccess
            ? Results.Created(locationFactory(result.Value), result.Value)
            : Problem(result.Error!);

    private static IResult Problem(Error error) => Results.Problem(
        detail: error.Message,
        statusCode: MapStatusCode(error.Type),
        title: TitleFor(error.Type),
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest,
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Solicitud inválida",
        ErrorType.NotFound => "Recurso no encontrado",
        ErrorType.Conflict => "Conflicto",
        ErrorType.Forbidden => "Acceso denegado",
        _ => "Error",
    };
}
