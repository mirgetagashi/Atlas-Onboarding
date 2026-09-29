using Atlas.Contracts;
using Atlas.Verification.Worker.Services;
using MassTransit;

namespace Atlas.Verification.Worker.Messaging;

/// <summary>Entry point for ApplicationSubmitted: runs the checks through the service and publishes the result.</summary>
public sealed class ApplicationSubmittedConsumer : IConsumer<ApplicationSubmitted>
{
    private readonly IVerificationService _verification;

    public ApplicationSubmittedConsumer(IVerificationService verification) => _verification = verification;

    public async Task Consume(ConsumeContext<ApplicationSubmitted> context)
    {
        var result = await _verification.VerifyAsync(context.Message, context.CancellationToken);
        if (result is not null)
        {
            await context.Publish(result, context.CancellationToken);
        }
    }
}
