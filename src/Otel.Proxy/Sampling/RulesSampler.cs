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
        }

        return Task.FromResult(decision);
    }

    private bool Equals(SampleCondition condition, object value)
    {
        if (condition.Value is string stringValue)
            return stringValue == value.ToString();

        return false;
    }
}

public class RulesSampler : BaseConditionsSampler, ISamplerRate
{
    private double _sampleRate;

    public RulesSampler(IEnumerable<SampleCondition> conditions, double sampleRate)
        : base(conditions.ToList())
    {
        _sampleRate = sampleRate;
    }

    public Task<double> GetSampleRate(string key)
    {
        return Task.FromResult(_sampleRate);
    }
}