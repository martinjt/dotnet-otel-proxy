using OpenTelemetry.Proto.Common.V1;

namespace Otel.Proxy.Sampling;
public class AverageRateSampler : BaseConditionsSampler, ISampler, ISamplerRateUpdater
{
    public double GoalSampleRate { get; }

    public string Name { get; }

    private readonly IAverageRateSamplerStore _samplerStore;
    private readonly HashSet<string> _attributesToUseForKey;

    public AverageRateSampler(IAverageRateSamplerStore samplerStore, string name, int goalSampleRate, HashSet<string> attributesToUseForKey)
        : base(Enumerable.Empty<SampleCondition>())
    {
        _samplerStore = samplerStore;
        Name = name;
        GoalSampleRate = goalSampleRate;
        _attributesToUseForKey = attributesToUseForKey;
    }
    public async Task<double> GetSampleRate(string key)
    {
        var sampleKeyInfo = await _samplerStore.GetSampleKeyInformation(key);
        return sampleKeyInfo.SampleRate;
    }

    public async Task UpdateAllSampleRates()
    {
        var sampleRates = await _samplerStore.GetAndResetSampleKeyInformation();
        var totalNumberOfTraces = sampleRates.Sum(x => x.CountOfInstances);
        var log10OfAllInstances = sampleRates.Sum(x => Math.Log10(x.CountOfInstances));

        var goalCount = totalNumberOfTraces / GoalSampleRate;
        var goalRatio = goalCount / log10OfAllInstances;

        var newSampleRates = CalculateSampleRates(
            sampleRates.ToDictionary(sr => sr.Key),
            goalRatio);
        
        await _samplerStore.UpdateAllSampleRates(newSampleRates.Values);
    }

    private static Dictionary<string, SampleKeyInformation> CalculateSampleRates(
        Dictionary<string, SampleKeyInformation> sampleRates,
        double goalRatio)
    {
        var sortedKeysAlphabetically = sampleRates.Keys
            .OrderBy(x => x)
            .ToList();

        var newSampleRates = new Dictionary<string, SampleKeyInformation>();
        var keysRemaining = sortedKeysAlphabetically.Count;
        var extra = 0.0;
        foreach (var key in sortedKeysAlphabetically)
        {
            // This code needs refactoring now that the tests are in.

            var count = Math.Max(1, sampleRates[key].CountOfInstances);
            var goalForKey = Math.Max(1, Math.Log10(count) * goalRatio);
            var extraForKey = extra / keysRemaining;
    		goalForKey += extraForKey;
    		extra -= extraForKey;
    		keysRemaining--;

            if (count <= goalForKey) {
                // there are fewer samples than the allotted number for this key. set
                // sample rate to 1 and redistribute the unused slots for future keys
                newSampleRates[key] = new SampleKeyInformation {
                    Key = key,
                    SampleRate = 1,
                    CountOfInstances = sampleRates[key].CountOfInstances 
                };
                extra += goalForKey - count;
            } else {
                // there are more samples than the allotted number. Sample this key enough
                // to knock it under the limit (aka round up)
                var rate = (int)Math.Ceiling(count / goalForKey);
                // if counts are <= 1 we can get values for goalForKey that are +Inf
                // and subsequent division ends up with NaN. If that's the case,
                // fall back to 1
                newSampleRates[key] = new SampleKeyInformation { Key = key, SampleRate = rate, CountOfInstances = sampleRates[key].CountOfInstances  };
	    		extra += goalForKey - (count / newSampleRates[key].SampleRate);
    		}
        }

        return newSampleRates;
    }

    public Task<string> GenerateKey(List<KeyValuePair<string, object>> tags)
    {
        var key = string.Join("|", tags
            .Where(x => _attributesToUseForKey.Contains(x.Key))
            .Select(x => x.Value is AnyValue value ? value.GetValueAsObject() : x.Value)
            .OrderBy(x => x));

        return Task.FromResult(key);
    }
}
