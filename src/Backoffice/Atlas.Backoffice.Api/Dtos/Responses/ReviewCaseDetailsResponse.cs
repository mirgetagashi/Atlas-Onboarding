using System.Text.Json;

namespace Atlas.Backoffice.Api.Dtos.Responses;

/// <summary>
/// The case plus the live application data, read from Onboarding in the customer's own market.
/// The application is passed through as JSON: Backoffice displays it but does not interpret it.
/// </summary>
public sealed record ReviewCaseDetailsResponse(ReviewCaseResponse Case, JsonElement Application);
