using OpenTelemetry.Proto.Common.V1;

namespace Otel.Proxy.Sampling;

public class BaseConditionsSampler
{
    public IEnumerable<SampleCondition> Conditions { get;}
    private readonly bool _hasSampleConditions = false;

    public BaseConditionsSampler(IEnumerable<SampleCondition> conditions)
    {
        Conditions = conditions;
        _hasSampleConditions = Conditions.Any();
    }

    public Task<bool> ShouldSample(List<KeyValuePair<string, object>> tags)
    {
        if (!_hasSampleConditions)
            return Task.FromResult(true);

        var existingTags = tags.Select(x => x.Key).ToHashSet();
        var decision = true;
        foreach (var sampleCondition in Conditions)
        {
            if (sampleCondition.Operator == ConditionsOperator.EqualTo && 
                existingTags.Contains(sampleCondition.Key) && 
                !EqualTo(sampleCondition, tags.FirstOrDefault(x => x.Key == sampleCondition.Key).Value))
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

    private bool GreaterThanOrEqualTo(SampleCondition condition, object value)
    {
        if (condition.Value is int intValue)
            return intValue > (value is AnyValue anyValue ? anyValue.IntValue : value as int?);

        if (condition.Value is double doubleValue)
            return doubleValue > (value is AnyValue anyValue ? anyValue.DoubleValue : value as double?);

        return false;
    }

    private bool EqualTo(SampleCondition condition, object value)
    {
        if (condition.Value is int intValue)
        {
            if (value is AnyValue anyValue)
                return intValue == anyValue.IntValue;
            return intValue == (int)value;
        }

        if (condition.Value is string stringValue)
        {
            if (value is AnyValue anyValue)
                return stringValue == anyValue.StringValue;
            return stringValue == (string)value;
        }
        
        return false;
    }
}
