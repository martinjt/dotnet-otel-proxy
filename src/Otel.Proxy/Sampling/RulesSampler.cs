namespace Otel.Proxy.Sampling;

public class ConsistentRateSampler : BaseConditionsSampler, ISampler
{
    private readonly double _sampleRate;
    public string Name { get; }

    public ConsistentRateSampler(string name, IEnumerable<SampleCondition> conditions, double sampleRate)
        : base(conditions.ToList())
    {
        Name = name;
        _sampleRate = sampleRate;
    }

    public Task<string> GenerateKey(List<KeyValuePair<string, object>> tags)
    {
        return Task.FromResult(string.Empty);
    }

    public Task<double> GetSampleRate(string key)
    {
        return Task.FromResult(_sampleRate);
    }
}