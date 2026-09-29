namespace Atlas.Backoffice.Api.Dtos.Responses;

public sealed record DevTokenResponse(string AccessToken, int ExpiresInSeconds);
