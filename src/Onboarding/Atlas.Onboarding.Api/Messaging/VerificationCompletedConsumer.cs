using Atlas.Contracts;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Services;
using MassTransit;

namespace Atlas.Onboarding.Api.Messaging;

/// <summary>
/// Entry point for the VerificationCompleted event. Like a controller, it only translates the
/// message into the domain's terms and calls the UseCases layer.
/// </summary>
public sealed class VerificationCompletedConsumer : IConsumer<VerificationCompleted>
{
    private readonly IVerificationResultService _verificationResults;

    public VerificationCompletedConsumer(IVerificationResultService verificationResults) =>
        _verificationResults = verificationResults;

    public Task Consume(ConsumeContext<VerificationCompleted> context) =>
        _verificationResults.ApplyAsync(context.Message.ApplicationId, ToOutcome(context.Message), context.CancellationToken);

    private static VerificationOutcome ToOutcome(VerificationCompleted message) => new(
        IdentityVerified: message.Identity == IdentityCheckOutcome.Verified,
        IdentityReference: message.IdentityReference,
        Screening: message.Screening switch
        {
            ScreeningOutcome.Clear => ScreeningResult.Clear,
            ScreeningOutcome.PossibleMatch => ScreeningResult.PossibleMatch,
            _ => null,
        },
        ScreeningReference: message.ScreeningReference);
}
