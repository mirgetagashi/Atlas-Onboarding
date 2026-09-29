using Atlas.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.ServiceDefaults.Http;

/// <summary>
/// Base class for every controller. Controllers only call a use-case service and pass its
/// <see cref="Result{T}"/> here; this is the one place where errors become HTTP responses.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value!) : FromError(result.Error!);

    protected IActionResult FromError(Error error)
    {
        if (error.Type == ErrorType.Validation && error.ValidationErrors is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(error.ValidationErrors.ToDictionary(e => e.Key, e => e.Value)));
        }

        if (error.Type == ErrorType.Forbidden)
        {
            return Forbid();
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodeFor(error.Type),
            Title = error.Title,
            Detail = error.Detail,
        };
        if (error.Code is not null)
        {
            problem.Extensions["code"] = error.Code;
        }

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" },
        };
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.UnprocessableEntity => StatusCodes.Status422UnprocessableEntity,
        ErrorType.UnsupportedMediaType => StatusCodes.Status415UnsupportedMediaType,
        ErrorType.PayloadTooLarge => StatusCodes.Status413PayloadTooLarge,
        ErrorType.LengthRequired => StatusCodes.Status411LengthRequired,
        _ => StatusCodes.Status500InternalServerError,
    };
}
