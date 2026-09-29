using Atlas.Contracts;

namespace Atlas.Verification.Worker.Services;

public interface IVerificationService
{
    /// <summary>
    /// Runs the automated checks for one application. Returns the result to publish,
    /// or null if the application was already checked (duplicate message).
    /// </summary>
    Task<VerificationCompleted?> VerifyAsync(ApplicationSubmitted submitted, CancellationToken cancellationToken);
}
