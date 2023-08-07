namespace Otel.Proxy.Samplers;
public class AverageRateRateSampler : ISampler
{
    private readonly int _goalSampleRate;
    private Dictionary<string, SampleKeyInformation> _sampleRates = new();

    public AverageRateRateSampler(int goalSampleRate)
    {
        _goalSampleRate = goalSampleRate;
    }
    public Task<int> GetSampleRate(string key)
    {
        if (!_sampleRates.ContainsKey(key))
            _sampleRates.Add(key, new SampleKeyInformation { SampleRate = _goalSampleRate, CountOfInstances = 1 });
        else
            _sampleRates[key].CountOfInstances++;

        return Task.FromResult(_sampleRates[key].SampleRate);
    }

    public Task UpdateAllSampleRates()
    {
        var totalNumberOfTraces = _sampleRates.Values.Sum(x => x.CountOfInstances);
        var log10OfAllInstances = _sampleRates.Values.Sum(x => Math.Log10(x.CountOfInstances));

        var goalCount = totalNumberOfTraces / _goalSampleRate;
        var goalRatio = goalCount / log10OfAllInstances;

        CalculateSampleRates(goalRatio);

        return Task.CompletedTask;
    }

    private void CalculateSampleRates(double goalRatio)
    {
        var sortedKeys = _sampleRates.Keys.OrderBy(k => k).ToList();

        var newSampleRates = new Dictionary<string, SampleKeyInformation>();
        var keysRemaining = sortedKeys.Count;
        var extra = 0.0;
        foreach (var key in sortedKeys)
        {
            var count = Math.Max(1, _sampleRates[key].CountOfInstances);
            var goalForKey = Math.Max(1, Math.Log10(count) * goalRatio);
            var extraForKey = extra / keysRemaining;
    		goalForKey += extraForKey;
    		extra -= extraForKey;
    		keysRemaining--;

            if (count <= goalForKey) {
                // there are fewer samples than the allotted number for this key. set
                // sample rate to 1 and redistribute the unused slots for future keys
                newSampleRates[key] = new SampleKeyInformation { 
                    SampleRate = 1,
                    CountOfInstances = _sampleRates[key].CountOfInstances 
                };
                extra += goalForKey - count;
            } else {
                // there are more samples than the allotted number. Sample this key enough
                // to knock it under the limit (aka round up)
                var rate = (int)Math.Ceiling(count / goalForKey);
                // if counts are <= 1 we can get values for goalForKey that are +Inf
                // and subsequent division ends up with NaN. If that's the case,
                // fall back to 1
                newSampleRates[key] = new SampleKeyInformation { SampleRate = rate, CountOfInstances = _sampleRates[key].CountOfInstances  };
	    		extra += goalForKey - (count / newSampleRates[key].SampleRate);
    		}
        }

        lock(_sampleRates) {
            _sampleRates = newSampleRates;
        }
    }

    private class SampleKeyInformation {
        public int SampleRate { get; set; }
        public int CountOfInstances { get; set; }
    }
}
