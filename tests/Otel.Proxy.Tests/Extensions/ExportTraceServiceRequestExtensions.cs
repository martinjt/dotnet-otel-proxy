using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using Shouldly;


namespace Otel.Proxy.Tests.Extensions;

internal static class ExportTraceServiceRequestExtensions
{
    public static List<ScopeSpans> GetSpansForService(this ExportTraceServiceRequest? exportRequest, string serviceName)
        => exportRequest?.ResourceSpans
            .Where(rs => rs.Resource
                           .Attributes.Any(a => a.Key == "service.name" && 
                                           a.Value.StringValue == serviceName))
            .SelectMany(rs => rs.ScopeSpans)
            .ToList()!;

    public static List<Span> GetAllSpansAsList(this ExportTraceServiceRequest? exportRequest)
        => exportRequest?.ResourceSpans
            .SelectMany(rs => rs.ScopeSpans)
            .SelectMany(ss => ss.Spans)
            .ToList()!;

    public static void AllSpansShouldHaveAttribute(this IEnumerable<Span> spans, string key, object value)
    {
        foreach (var span in spans)
        {
            var foundAttribute = span.Attributes.FirstOrDefault(a => a.Key == key);
            if (foundAttribute == null)
                Assert.Fail($"Span {span.Name} does not have attribute {key}");

            object? foundValue = value switch {
                string s => foundAttribute.Value.StringValue,
                int i => foundAttribute.Value.IntValue,
                _ => null
            };
            if (foundValue == null)
                Assert.Fail($"Span {span.Name} does not have attribute {key} with value {value}");
            
            foundValue.ShouldBe(value);
        }
    }
}