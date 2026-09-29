namespace Atlas.Verification.Worker.Clients.Models;

/// <summary>What is sent to both providers: the minimum needed to identify and screen the person.</summary>
public sealed record ProviderRequest(string Reference, string FirstName, string LastName, DateOnly DateOfBirth, string Country);
