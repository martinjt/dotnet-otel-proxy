using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

internal static partial class TraceGenerator
{
    public static ExportTraceServiceRequest CreateValidTraceExport(string serviceName = "test-service")
    {
        var exportRequest = new ExportTraceServiceRequest();
        var resource = new Resource();
        resource.Attributes.Add(new KeyValue
        {
            Key = "service.name",
            Value = new AnyValue { StringValue = serviceName }
        });
        var spans = new ScopeSpans();
        spans.Spans.Add(new Span
        {
            TraceId = GenerateRandomTraceId(),
            SpanId = GenerateRandomSpanId(),
            Attributes = {
                { new KeyValue {
                    Key = "my_attribute",
                    Value = new AnyValue { StringValue = "my value"}
                }}
            }
        });
        var resourceSpans = new ResourceSpans
        {
            Resource = resource
        };
        resourceSpans.ScopeSpans.Add(spans);
        exportRequest.ResourceSpans.Add(resourceSpans);

        return exportRequest;
    }

}