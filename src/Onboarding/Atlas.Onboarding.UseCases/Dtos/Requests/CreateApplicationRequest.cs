namespace Atlas.Onboarding.UseCases.Dtos.Requests;

/// <summary>Body of POST /applications. Same fields as the ticket, minus the images and terms (separate steps).</summary>
public sealed record CreateApplicationRequest(
    string? Market,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? Email,
    string? Phone,
    IdentifierRequest? Identifier);
