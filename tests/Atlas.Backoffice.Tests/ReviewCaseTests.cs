using Atlas.Backoffice.Api.Domain;

namespace Atlas.Backoffice.Tests;

public sealed class ReviewCaseTests
{
    private static readonly DateTimeOffset ReferredAt = new(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_case_is_due_48_hours_after_referral()
    {
        var reviewCase = ReviewCase.Open("MB-0123456789abcdef0123456789abcdef", "mb", ReferredAt);

        Assert.Equal(ReviewCaseStatus.Open, reviewCase.Status);
        Assert.Equal("MB", reviewCase.Market);
        Assert.Equal(ReferredAt.AddHours(48), reviewCase.DueBy);
    }

    [Fact]
    public void An_open_case_past_its_due_time_is_overdue()
    {
        var reviewCase = ReviewCase.Open("MB-0123456789abcdef0123456789abcdef", "MB", ReferredAt);

        Assert.False(reviewCase.IsOverdue(ReferredAt.AddHours(47)));
        Assert.True(reviewCase.IsOverdue(ReferredAt.AddHours(49)));
    }

    [Fact]
    public void Recording_the_same_decision_twice_is_harmless()
    {
        var reviewCase = ReviewCase.Open("MB-0123456789abcdef0123456789abcdef", "MB", ReferredAt);
        reviewCase.RecordDecision(ReviewOutcome.Reject, "officer.anna", ReferredAt.AddHours(2));

        reviewCase.RecordDecision(ReviewOutcome.Reject, "officer.anna", ReferredAt.AddHours(3));

        Assert.Equal(ReviewCaseStatus.Decided, reviewCase.Status);
        Assert.Equal(ReferredAt.AddHours(2), reviewCase.DecidedAt);
        Assert.False(reviewCase.IsOverdue(ReferredAt.AddHours(100)));
    }

    [Fact]
    public void A_decision_cannot_be_changed()
    {
        var reviewCase = ReviewCase.Open("MB-0123456789abcdef0123456789abcdef", "MB", ReferredAt);
        reviewCase.RecordDecision(ReviewOutcome.Reject, "officer.anna", ReferredAt.AddHours(2));

        Assert.Throws<InvalidOperationException>(() =>
            reviewCase.RecordDecision(ReviewOutcome.Approve, "officer.bora", ReferredAt.AddHours(3)));
    }
}
