using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.Onboarding.Api.ErrorHandling;

/// <summary>
/// Turns domain exceptions into HTTP responses: an action in the wrong status or a concurrent
/// update gives 409, any other broken business rule gives 422.
/// </summary>
internal sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public DomainExceptionHandler(IProblemDetailsService problemDetails) => _problemDetails = problemDetails;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            InvalidStateTransitionException e => Problem(StatusCodes.Status409Conflict, "Action not allowed in the current status", e.Message, e.Code),
            DomainException e => Problem(StatusCodes.Status422UnprocessableEntity, "Business rule violated", e.Message, e.Code),
            ConcurrencyConflictException e => Problem(StatusCodes.Status409Conflict, "Concurrent update", e.Message, "concurrent_update"),
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        return problem;
    }
}
