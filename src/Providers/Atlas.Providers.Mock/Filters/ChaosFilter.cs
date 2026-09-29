using Atlas.Providers.Mock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Atlas.Providers.Mock.Filters;

/// <summary>Applied to the provider controllers: adds latency and fails a share of calls, as configured in <see cref="ChaosSettings"/>.</summary>
public sealed class ChaosFilter : IAsyncActionFilter
{
    private readonly ChaosSettings _settings;

    public ChaosFilter(ChaosSettings settings) => _settings = settings;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var chaos = _settings.Snapshot();
        await Task.Delay(chaos.LatencyMs, context.HttpContext.RequestAborted);

        if (Random.Shared.NextDouble() < chaos.FailureRate)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Provider unavailable (simulated)",
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
            return;
        }

        await next();
    }
}
