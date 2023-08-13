using OpenTelemetry.Proto.Common.V1;

internal class GenericSampleKeyGenerator
{
    private readonly HashSet<string> _attributesToUseForKey;

    public GenericSampleKeyGenerator(HashSet<string> attributesToUseForKey)
    {
        _attributesToUseForKey = attributesToUseForKey;
    }

    public Task<string> GenerateKey(List<KeyValuePair<string, AnyValue>> tags)
    {
        var key = string.Join("|", tags
            .Where(x => _attributesToUseForKey.Contains(x.Key))
            .Select(x => x.Value.GetValueAsString())
            .OrderBy(x => x));

        return Task.FromResult(key);
    }
}

internal static class OpenTelemetryExtensions
{
    public static string GetValueAsString(this AnyValue anyValue)
    {
        var value = anyValue.ValueCase switch
        {
            AnyValue.ValueOneofCase.StringValue => anyValue.StringValue,
            AnyValue.ValueOneofCase.BoolValue => anyValue.BoolValue.ToString(),
            AnyValue.ValueOneofCase.IntValue => anyValue.IntValue.ToString(),
            AnyValue.ValueOneofCase.DoubleValue => anyValue.DoubleValue.ToString(),
            AnyValue.ValueOneofCase.BytesValue => anyValue.BytesValue.ToString(),
            AnyValue.ValueOneofCase.KvlistValue => string.Join(",",
                anyValue.KvlistValue
                        .Values
                        .Select(x => x.Value.GetValueAsString())),
            AnyValue.ValueOneofCase.None => string.Empty,
            AnyValue.ValueOneofCase.ArrayValue => string.Join(",",
                anyValue.ArrayValue
                        .Values
                        .Select(x => x.GetValueAsString())),
            _ => string.Empty
        };

        return value!;

    }

}