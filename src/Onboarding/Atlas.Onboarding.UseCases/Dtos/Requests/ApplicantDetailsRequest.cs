namespace Atlas.Onboarding.UseCases.Dtos.Requests;

// Every field is nullable: JSON can omit anything, and validation reports it instead of crashing.
public sealed record ApplicantDetailsRequest(
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? Email,
    string? Phone,
    IdentifierRequest? Identifier);
