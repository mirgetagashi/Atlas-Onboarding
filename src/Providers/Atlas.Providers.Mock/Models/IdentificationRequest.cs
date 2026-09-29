namespace Atlas.Providers.Mock.Models;

public sealed record IdentificationRequest(string Reference, string FirstName, string LastName, DateOnly DateOfBirth, string Country);
