using OpenTelemetry.Proto.Common.V1;

namespace Otel.Proxy.Processing
{
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

        public static object GetValueAsObject(this AnyValue anyValue)
        {
            object value = anyValue.ValueCase switch
            {
                AnyValue.ValueOneofCase.StringValue => anyValue.StringValue,
                AnyValue.ValueOneofCase.BoolValue => anyValue.BoolValue,
                AnyValue.ValueOneofCase.IntValue => anyValue.IntValue,
                AnyValue.ValueOneofCase.DoubleValue => anyValue.DoubleValue,
                AnyValue.ValueOneofCase.BytesValue => anyValue.BytesValue,
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

        public static void AddAttributeToAllSpans(this IEnumerable<SpanRecord> trace, string key, object value)
        {
            var keyValue = new KeyValue
            {
                Key = key,
                Value = new AnyValue()
            };
            switch (value)
            {
                case string s:
                    keyValue.Value.StringValue = s;
                    break;
                case int i:
                    keyValue.Value.IntValue = i;
                    break;
                default:
                    throw new NotImplementedException();
            }
            foreach (var span in trace)
            {
                span.Spans.ForEach(s => s.Attributes.Add(keyValue));
            }
        }
}
}