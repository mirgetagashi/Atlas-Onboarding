using System.Diagnostics.CodeAnalysis;
using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Abstractions;

namespace Atlas.Onboarding.UseCases.Shared;

internal static class ApplicationLookup
{
    /// <summary>Reads the market out of the id. Malformed ids and unknown markets both end up as "not found".</summary>
    public static bool TryResolve(
        string rawId,
        IMarketCatalog markets,
        out ApplicationId id,
        [NotNullWhen(true)] out MarketDefinition? market)
    {
        market = null;
        return ApplicationId.TryParse(rawId, out id) && markets.TryGet(id.Market, out market);
    }

    /// <summary>Returns the application only if the applicant token matches; otherwise null, so existence is not revealed.</summary>
    public static async Task<Application?> FindForApplicantAsync(
        this IMarketUnitOfWork unitOfWork,
        ApplicationId id,
        string? applicantToken,
        CancellationToken cancellationToken)
    {
        var application = await unitOfWork.FindApplicationAsync(id, cancellationToken);
        return application is not null && ApplicantToken.Matches(applicantToken, application.ApplicantTokenHash)
            ? application
            : null;
    }
}
