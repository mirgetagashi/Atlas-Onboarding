namespace Atlas.Backoffice.Api.Dtos.Requests;

public sealed record DevTokenRequest(string? UserId, string? Role, string? Market);
