namespace Otel.Proxy.Samplers.AverageRateSamplers.Tests;

public class AverageRateSamplerTests
{
    private AverageRateRateSampler _sut;

    public AverageRateSamplerTests()
    {
        _sut = new AverageRateRateSampler(20);
    }
    
    [Fact]
    public async Task NoDataInSampler_ReturnsDefaultSampleRate()
    {
        Assert.Equal(20, await _sut.GetSampleRate("test"));
    }

    [Fact]
    public async Task ValidData_AfterUpdateSampleRates_NewSampleRatesAreReturns()
    {
        var sampleData = new List<SamplerTestDataObject>() {
            new ("one",   1, 1),
            new ("two",   1, 1),
            new ("three", 2, 1),
            new ("four",  5, 1),
            new ("five",  8, 1),
            new ("six",   15, 1),
            new ("seven", 45, 1),
            new ("eight", 612, 6),
            new ("nine",  2000, 14),
            new ("ten",   10000, 47)
        };
        await SetSampleData(sampleData);

        await _sut.UpdateAllSampleRates();

        await AssertSampleRates(sampleData);
    }


    private async Task SetSampleData(List<SamplerTestDataObject> sampleData)
    {
        foreach (var data in sampleData)
        {
            for (var i = 0; i < data.OriginalSampleRate; i++)
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
}


// samplertestdataobject as a record
public record SamplerTestDataObject(string Key, int OriginalSampleRate, int NewSampleRate);
