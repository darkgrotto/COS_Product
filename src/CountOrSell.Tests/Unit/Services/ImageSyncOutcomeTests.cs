using CountOrSell.Domain.Dtos;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

// Images are best-effort: a failure never fails the update. That is only defensible if the
// shortfall is reported, so these cover the reporting contract rather than the fetching.
public class ImageSyncOutcomeTests
{
    [Fact]
    public void Complete_When_Every_Listed_Image_Was_Saved()
    {
        var outcome = new ImageSyncOutcome(Listed: 101288, Saved: 101288, 0, 0, 0, 0);

        Assert.True(outcome.IsComplete);
        Assert.Equal(0, outcome.Missing);
    }

    [Fact]
    public void Incomplete_When_Any_Image_Is_Missing()
    {
        // The case that used to report plain success: a full package where the request budget
        // ran out partway, leaving a visibly incomplete collection and no indication why.
        var outcome = new ImageSyncOutcome(
            Listed: 101288, Saved: 100000, SkippedChecksum: 0, RejectedPath: 0,
            Failed: 1288, RateLimited: 1288);

        Assert.False(outcome.IsComplete);
        Assert.Equal(1288, outcome.Missing);
    }

    [Fact]
    public void No_Images_Listed_Is_Complete_Not_A_Shortfall()
    {
        // Delta packages carry no images at all - that is normal, not a failure.
        Assert.True(ImageSyncOutcome.None.IsComplete);
        Assert.Equal(0, ImageSyncOutcome.None.Missing);
    }

    [Fact]
    public void Explain_Leads_With_Rate_Limiting()
    {
        // Rate limiting is singled out because it is the one cause that is external and
        // fixable by the operator rather than a problem with the package.
        var outcome = new ImageSyncOutcome(
            Listed: 100, Saved: 40, SkippedChecksum: 5, RejectedPath: 5, Failed: 50, RateLimited: 30);

        Assert.Contains("429", outcome.Explain());
        Assert.Contains("30", outcome.Explain());
    }

    [Fact]
    public void Explain_Falls_Back_Through_The_Other_Causes()
    {
        Assert.Contains("could not be fetched",
            new ImageSyncOutcome(100, 90, 0, 0, Failed: 10, RateLimited: 0).Explain());

        Assert.Contains("checksum",
            new ImageSyncOutcome(100, 90, SkippedChecksum: 10, RejectedPath: 0, Failed: 0, RateLimited: 0).Explain());

        Assert.Contains("unusable paths",
            new ImageSyncOutcome(100, 90, SkippedChecksum: 0, RejectedPath: 10, Failed: 0, RateLimited: 0).Explain());
    }
}
