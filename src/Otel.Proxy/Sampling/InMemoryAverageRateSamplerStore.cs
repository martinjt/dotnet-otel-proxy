using System.Collections.Concurrent;

namespace Otel.Proxy.Sampling;

public interface IAverageRateSamplerStore
{
    public Task<SampleKeyInformation> GetSampleKeyInformation(string key, int instances = 1);
    public Task<IEnumerable<SampleKeyInformation>> GetAndResetSampleKeyInformation();
    public Task UpdateAllSampleRates(IEnumerable<SampleKeyInformation> keyInfos);
}

public class InMemoryAverageRateSamplerStore : IAverageRateSamplerStore
{
    private ConcurrentDictionary<string, SampleKeyInformation> _sampleRates = new();
    private readonly int _defaultSampleRate;


    public InMemoryAverageRateSamplerStore(int defaultSampleRate)
    {
        _defaultSampleRate = defaultSampleRate;
    }

    public Task<SampleKeyInformation> GetSampleKeyInformation(string key, int instances = 1)
    {
        var keyInfo = _sampleRates.AddOrUpdate(key, (k) => new SampleKeyInformation { 
            Key = key,
            SampleRate = _defaultSampleRate,
            CountOfInstances = instances 
            },
            (key, sampleKeyInformation) => {
                sampleKeyInformation.CountOfInstances += instances;
                return sampleKeyInformation;
            });

        return Task.FromResult(keyInfo);
    }

    public Task<IEnumerable<SampleKeyInformation>> GetAndResetSampleKeyInformation()
    {
        Dictionary<string, SampleKeyInformation> copyOfSampleRates;
        lock (_sampleRates)
        {
            copyOfSampleRates = new Dictionary<string, SampleKeyInformation>(_sampleRates);
            _sampleRates = new ConcurrentDictionary<string, SampleKeyInformation>();
        }
        return Task.FromResult(copyOfSampleRates.Values.AsEnumerable());
    }

    public Task UpdateAllSampleRates(IEnumerable<SampleKeyInformation> keyInfos)
    {
        foreach (var key in keyInfos)
        {
            _sampleRates.AddOrUpdate(key.Key, (k) => new SampleKeyInformation
            {
                Key = key.Key,
                SampleRate = key.SampleRate,
                CountOfInstances = 0
            },
            (k, sampleKeyInformation) => {
                sampleKeyInformation.SampleRate = key.SampleRate;
                return sampleKeyInformation;
            });

        }
        return Task.CompletedTask;
    }
}


public class SampleKeyInformation
{
    public string Key { get; internal set; }
    public double SampleRate { get; internal set; }
    public int CountOfInstances { get; internal set; }
}