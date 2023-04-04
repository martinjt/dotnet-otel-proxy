using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

public class TraceRepository
{
    public Dictionary<byte[], SpanRecord> SpanDictionary = new();

    public void AddSpan(ExportTraceServiceRequest request)
    {
        foreach (var resourceSpan in request.ResourceSpans)
        foreach (var grouping in resourceSpan
            .ScopeSpans.SelectMany(ss => ss.Spans)
            .GroupBy(s => s.TraceId))
            {
                var traceId = grouping.Key;
                var record = new SpanRecord
                {
                    Spans = grouping.ToList(),
                    Resource = resourceSpan.Resource
                };
                SpanDictionary.Add(traceId, record);
            }
    }
}

public class SpanRecord
{
    public List<Span> Spans { get; set; } = new();
    public Resource Resource { get; set; } = new();
}

