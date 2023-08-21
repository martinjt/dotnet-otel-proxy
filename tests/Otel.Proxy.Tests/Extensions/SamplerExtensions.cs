namespace Otel.Proxy.Tests.Extensions;

public static class SamplerExtensions
{
    public static Dictionary<string, object> ConditionsAsDictionary(this BaseConditionsSampler sampler)
    {
        return sampler.Conditions
            .Select(c => new KeyValuePair<string, object>(c.Key, c.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}