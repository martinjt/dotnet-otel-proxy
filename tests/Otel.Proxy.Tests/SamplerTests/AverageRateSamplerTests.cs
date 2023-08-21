namespace Otel.Proxy.Tests.SamplerTests;

public class AverageRateSamplerTests
{
    private readonly AverageRateSampler _sut
        = new (new InMemoryAverageRateSamplerStore(20), "dummy", 20, new());

    [Fact]
    public async Task NoDataInSampler_ReturnsDefaultSampleRate()
    {
        Assert.Equal(20, await _sut.GetSampleRate("test"));
    }

    [Fact]
    public async Task SingleKeyWithCountBelowSampleRate_SampleRateIsSetToTraceCount()
    {
        var currentTraceCount = (int)_sut.GoalSampleRate - 1;
        var sampleData = new List<SamplerTestDataObject>{
            new (RandomKey(), currentTraceCount, currentTraceCount),
        };

        await SetSampleData(sampleData);

        await _sut.UpdateAllSampleRates();

        await AssertSampleRates(sampleData);
    }

    [Fact]
    public async Task SingleKeyWithCountAboveGoalSampleRate_SampleRateIsSetToGoalSampleRate()
    {
        var currentTraceCount = (int)_sut.GoalSampleRate + 20;
        var sampleData = new List<SamplerTestDataObject>{
            new (RandomKey(), currentTraceCount, _sut.GoalSampleRate),
        };

        await SetSampleData(sampleData);

        await _sut.UpdateAllSampleRates();

        await AssertSampleRates(sampleData);
    }

    [Fact]
    public async Task UnequalDistributionOfCountsMuchGreaterThanGoalWithSpecificKeyStrings_SampleRateIsAccurate()
    {
        var sampleData = new List<SamplerTestDataObject>() {
            new ("one", 1, 1),
            new ("two", 1, 1),
            new ("three", 2, 1),
            new ("four", 5, 1),
            new ("five", 8, 1),
            new ("six", 15, 1),
            new ("seven", 45, 1),
            new ("eight", 612, 6),
            new ("nine", 2000, 14),
            new ("ten", 10000, 47)
        };
        await SetSampleData(sampleData);

        await _sut.UpdateAllSampleRates();

        await AssertSampleRates(sampleData);
    }

    [Fact]
    public async Task AllCountsUnderneathGoalRate_NewSampleRateIsEqualToCount()
    {
        var sampleData = new List<SamplerTestDataObject>() {
                new (RandomKey(), 1,1),
                new (RandomKey(), 1,1),
                new (RandomKey(), 2,2),
                new (RandomKey(), 5,5),
                new (RandomKey(), 7,7)
        };
        await SetSampleData(sampleData);

        await _sut.UpdateAllSampleRates();

        await AssertSampleRates(sampleData);
    }

    [Fact]
    public async Task AllCountsTheSameAboveGoalSampleRate_AllKeysSetToGoalSampleRate()
    {
        var originalSampleRate = 6000;
        var sampleData = new List<SamplerTestDataObject>{
            new (RandomKey(), originalSampleRate, _sut.GoalSampleRate),
            new (RandomKey(), originalSampleRate, _sut.GoalSampleRate),
            new (RandomKey(), originalSampleRate, _sut.GoalSampleRate),
            new (RandomKey(), originalSampleRate, _sut.GoalSampleRate),
            new (RandomKey(), originalSampleRate, _sut.GoalSampleRate)
        };
        await SetSampleData(sampleData);

        await _sut.UpdateAllSampleRates();

        await AssertSampleRates(sampleData);
    }

    [Fact]
    public async Task TwentyDifferentKeysOnlyOneNotTheSameButBelowGoalSampleRate_ChangesOnlyThatOne()
    {
        var sampleData = new List<SamplerTestDataObject>()
        {
            new (RandomKey(), 10, 7),
        };
        sampleData.AddRange(
            Enumerable.Range(0, 19)
                .Select(_ => new SamplerTestDataObject(RandomKey(), 1, 1))
        );

        await SetSampleData(sampleData);

        await _sut.UpdateAllSampleRates();

        await AssertSampleRates(sampleData);
    }

    private async Task SetSampleData(List<SamplerTestDataObject> sampleData)
    {
        foreach (var data in sampleData)
        {
            for (var i = 0; i < data.CurrentTraceCount; i++)
                await _sut.GetSampleRate(data.Key);
        }
    }

    private async Task AssertSampleRates(List<SamplerTestDataObject> sampleData)
    {
        foreach (var data in sampleData)
        {
            Assert.Equal(data.NewSampleRate, await _sut.GetSampleRate(data.Key));
        }
    }

    private static string RandomKey() => Guid.NewGuid().ToString();

    public record SamplerTestDataObject(string Key, int CurrentTraceCount, double NewSampleRate);
}


