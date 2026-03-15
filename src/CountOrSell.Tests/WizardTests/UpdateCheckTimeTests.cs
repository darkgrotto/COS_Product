using CountOrSell.Wizard.Services;
using Xunit;

namespace CountOrSell.Tests.WizardTests;

public class UpdateCheckTimeTests
{
    [Fact]
    public void GenerateUpdateCheckTime_ProducesValidFormat()
    {
        var time = UpdateCheckTimeGenerator.Generate();

        Assert.Matches(@"^\d{2}:\d{2}$", time);
        var parts = time.Split(':');
        Assert.Equal(2, parts.Length);
        Assert.InRange(int.Parse(parts[0]), 0, 23);
        Assert.InRange(int.Parse(parts[1]), 0, 59);
    }

    [Fact]
    public void GenerateUpdateCheckTime_ProducesDifferentValues()
    {
        var times = Enumerable.Range(0, 10)
            .Select(_ => UpdateCheckTimeGenerator.Generate())
            .ToList();

        // Verify format HH:MM for all
        foreach (var t in times)
        {
            Assert.Matches(@"^\d{2}:\d{2}$", t);
            var parts = t.Split(':');
            Assert.InRange(int.Parse(parts[0]), 0, 23);
            Assert.InRange(int.Parse(parts[1]), 0, 59);
        }

        // Not all identical - with 10 samples from 1440 possibilities
        // the probability of all being identical is astronomically small
        var distinct = times.Distinct().Count();
        Assert.True(distinct > 1,
            "All 10 generated times were identical - random generation may be broken");
    }

    [Fact]
    public void GenerateUpdateCheckTime_HourIsZeroPadded()
    {
        // Run many times and verify hour is always 2-digit (00-23)
        for (int i = 0; i < 100; i++)
        {
            var time = UpdateCheckTimeGenerator.Generate();
            var hourStr = time.Split(':')[0];
            Assert.Equal(2, hourStr.Length);
        }
    }

    [Fact]
    public void GenerateUpdateCheckTime_MinuteIsZeroPadded()
    {
        // Run many times and verify minute is always 2-digit (00-59)
        for (int i = 0; i < 100; i++)
        {
            var time = UpdateCheckTimeGenerator.Generate();
            var minuteStr = time.Split(':')[1];
            Assert.Equal(2, minuteStr.Length);
        }
    }
}
