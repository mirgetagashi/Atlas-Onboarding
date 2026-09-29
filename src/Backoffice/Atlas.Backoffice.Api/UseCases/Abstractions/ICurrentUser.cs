namespace Atlas.Backoffice.Api.UseCases.Abstractions;

/// <summary>The logged-in staff member. Implemented from the JWT; the use cases never see HTTP or claims.</summary>
public interface ICurrentUser
{
    string Id { get; }

    /// <summary>The market this person works in, or null if the token has none.</summary>
    string? Market { get; }

    bool CanAccessMarket(string market);
}
