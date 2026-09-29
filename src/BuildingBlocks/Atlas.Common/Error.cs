namespace Atlas.Common;

/// <summary>What kind of failure happened. The API layer turns each kind into an HTTP status code.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Forbidden,
    Conflict,
    UnprocessableEntity,
    UnsupportedMediaType,
    PayloadTooLarge,
    LengthRequired,
}

/// <summary>
/// An expected failure returned by a use case (bad input, not found, not allowed...).
/// Unexpected failures are still exceptions; expected ones are values, so callers must handle them.
/// </summary>
public sealed record Error(
    ErrorType Type,
    string Title,
    string? Detail = null,
    string? Code = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public static readonly Error Forbidden = new(ErrorType.Forbidden, "You are not allowed to access this resource.");

    public static Error NotFound(string? detail = null) => new(ErrorType.NotFound, "Not found.", detail);

    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new(ErrorType.Validation, "One or more validation errors occurred.", ValidationErrors: errors);

    public static Error Validation(string field, string message) =>
        Validation(new Dictionary<string, string[]> { [field] = new[] { message } });

    public static Error Conflict(string title, string? detail = null, string? code = null) =>
        new(ErrorType.Conflict, title, detail, code);
}
