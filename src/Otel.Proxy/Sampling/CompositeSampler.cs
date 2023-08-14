namespace Otel.Proxy.Sampling;

public class CompositeSampler 
{
    private readonly IEnumerable<ISampler> _samplers;
    private readonly int _defaultSampleRate;

    public bool SamplingActive { get; } = false;

    public CompositeSampler(IEnumerable<ISampler> samplers, int defaultSampleRate = 1)
    {
        _samplers = samplers;
        _defaultSampleRate = defaultSampleRate;
        SamplingActive = _samplers.Any();
    }

    public async Task<double> GetSampleRate(List<KeyValuePair<string, object>> attributes)
    {
        foreach (var sampler in _samplers)
        {
            if (!await sampler.ShouldSample(attributes))
            {   
                var key = await sampler.GenerateKey(attributes);
                return await sampler.GetSampleRate(key);
            }
        }
        return _defaultSampleRate;
    }
}