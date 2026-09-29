namespace Atlas.Providers.Mock.Models;

public sealed record ScreeningRequest(string Reference, string FirstName, string LastName, DateOnly DateOfBirth, string Country);
