namespace Otel.Proxy.Sampling;

public class BaseConditionsSampler
{
    private readonly IEnumerable<SampleCondition> _conditions;
    private readonly bool _hasSampleConditions = false;

    public BaseConditionsSampler(IEnumerable<SampleCondition> conditions)
    {
        _conditions = conditions;
        _hasSampleConditions = _conditions.Any();
    }

    public Task<bool> ShouldSample(List<KeyValuePair<string, object>> tags)
    {
        if (!_hasSampleConditions)
            return Task.FromResult(true);

        var existingTags = tags.Select(x => x.Key).ToHashSet();
        var decision = true;
        foreach (var sampleCondition in _conditions)
        {
            if (sampleCondition.Operator == ConditionsOperator.Equals && 
                existingTags.Contains(sampleCondition.Key) && 
                !Equals(sampleCondition, tags.FirstOrDefault(x => x.Key == sampleCondition.Key).Value))
                {
                    decision = false;
                    break;
                }
            if (sampleCondition.Operator == ConditionsOperator.GreaterThanOrEqualTo && 
                existingTags.Contains(sampleCondition.Key) && 
                !GreaterThanOrEqualTo(sampleCondition, tags.FirstOrDefault(x => x.Key == sampleCondition.Key).Value))
                {
                    decision = false;
                    break;
                }
        }

        return Task.FromResult(decision);
    }

    private bool GreaterThanOrEqualTo(SampleCondition sampleCondition, object value)
    {
        if (sampleCondition.Value is int intValue)
            return intValue > (int)value;

        if (sampleCondition.Value is double doubleValue)
            return doubleValue > (double)value;

        return false;
    }

    private bool Equals(SampleCondition condition, object value)
    {
        if (condition.Value is string stringValue)
            return stringValue == value.ToString();

        return false;
    }
}

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