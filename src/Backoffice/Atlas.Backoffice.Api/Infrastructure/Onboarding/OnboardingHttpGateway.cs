using System.Net;
using System.Text.Json;
using Atlas.Backoffice.Api.Domain;
using Atlas.Backoffice.Api.Dtos.Responses;
using Atlas.Backoffice.Api.UseCases.Abstractions;
using Atlas.Common;

namespace Atlas.Backoffice.Api.Infrastructure.Onboarding;

/// <summary>
/// HTTP implementation of <see cref="IOnboardingGateway"/>. Onboarding's error responses are turned
/// back into <see cref="Error"/> values, so the caller sees the same 400/403/404/409 Onboarding gave.
/// </summary>
internal sealed class OnboardingHttpGateway : IOnboardingGateway
{
    private readonly HttpClient _http;

    public OnboardingHttpGateway(HttpClient http) => _http = http;

    public async Task<Result<JsonElement>> GetApplicationAsync(string applicationId, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync($"internal/applications/{Escape(applicationId)}", cancellationToken);
        return await ReadJsonAsync(response, cancellationToken);
    }

    public async Task<Result<DocumentContent>> GetDocumentAsync(string applicationId, string documentType, CancellationToken cancellationToken)
    {
        var response = await _http.GetAsync(
            $"internal/applications/{Escape(applicationId)}/documents/{Escape(documentType)}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                return await ToErrorAsync(response, cancellationToken);
            }
        }

        // Not disposed here: disposing the returned stream (done when the HTTP response is sent) releases the connection.
        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        return new DocumentContent(content, contentType);
    }

    public async Task<Result<JsonElement>> RecordComplianceDecisionAsync(
        string applicationId,
        ReviewOutcome outcome,
        string reason,
        CancellationToken cancellationToken)
    {
        var body = new { decision = outcome == ReviewOutcome.Approve ? "APPROVE" : "REJECT", reason };
        using var response = await _http.PostAsJsonAsync(
            $"internal/applications/{Escape(applicationId)}/compliance-decision",
            body,
            cancellationToken);
        return await ReadJsonAsync(response, cancellationToken);
    }

    public async Task<Result<JsonElement>> ConfirmBranchActivationAsync(
        string applicationId,
        string? branchId,
        bool wetSignatureConfirmed,
        CancellationToken cancellationToken)
    {
        var body = new { branchId, wetSignatureConfirmed };
        using var response = await _http.PostAsJsonAsync(
            $"internal/applications/{Escape(applicationId)}/branch-activation",
            body,
            cancellationToken);
        return await ReadJsonAsync(response, cancellationToken);
    }

    private static async Task<Result<JsonElement>> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return await ToErrorAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    /// <summary>Expected answers become <see cref="Error"/> values; a 5xx means Onboarding itself failed, so it stays an exception.</summary>
    private static async Task<Error> ToErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await TryReadProblemAsync(response, cancellationToken);
        var title = problem?.Title ?? "The request to Onboarding failed.";

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest when problem?.Errors is { Count: > 0 } errors => Error.Validation(errors),
            HttpStatusCode.BadRequest => new Error(ErrorType.Validation, title, problem?.Detail),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => Error.Forbidden,
            HttpStatusCode.NotFound => Error.NotFound(problem?.Detail),
            HttpStatusCode.Conflict => Error.Conflict(title, problem?.Detail),
            HttpStatusCode.UnprocessableEntity => new Error(ErrorType.UnprocessableEntity, title, problem?.Detail),
            _ => throw new HttpRequestException($"Onboarding answered {(int)response.StatusCode}.", null, response.StatusCode),
        };
    }

    private static async Task<ProblemPayload?> TryReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemPayload>(cancellationToken);
        }
        catch (JsonException)
        {
            return null; // empty or non-JSON body
        }
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);

    private sealed record ProblemPayload(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
