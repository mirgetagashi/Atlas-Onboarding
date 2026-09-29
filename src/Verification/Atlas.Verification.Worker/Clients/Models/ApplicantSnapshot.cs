namespace Atlas.Verification.Worker.Clients.Models;

/// <summary>The fields this worker needs from Onboarding. Defined here, not shared, so the two services evolve separately.</summary>
public sealed record ApplicantSnapshot(
    string ApplicationId,
    string Market,
    string Status,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth);
