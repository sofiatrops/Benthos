namespace Bep.Application.Abstractions;

/// <summary>
/// Resultado de un caso de uso. Evita usar excepciones para el flujo de negocio
/// esperado (fallos de validación, conflictos), reservando las excepciones para
/// lo verdaderamente excepcional.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("Un resultado correcto no puede tener error.");
        }

        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException("Un resultado fallido requiere un error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);

    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>Resultado que transporta un valor cuando es correcto.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error)
        => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");
}

/// <summary>Error de negocio con código estable (para i18n y trazabilidad) y mensaje.</summary>
/// <param name="Code">Código estable del error (p. ej. <c>organization.empresa.rut_duplicado</c>).</param>
/// <param name="Message">Mensaje legible.</param>
/// <param name="Type">Categoría del error, para mapear al código HTTP en la capa de presentación.</param>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
}
