using System.Collections.Concurrent;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

internal class InMemoryTraceRepository : ITraceRepository
{
    private ConcurrentDictionary<string, ConcurrentBag<SpanRecord>> SpanDictionary = new();

    public void AddSpans(ExportTraceServiceRequest request)
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
                var records = SpanDictionary.GetOrAdd(System.Text.Encoding.UTF8.GetString(traceId.Memory.ToArray()),  
                    (b) => new ConcurrentBag<SpanRecord>());
                records.Add(record);
            }
    }

    public IEnumerable<SpanRecord> GetTrace(byte[] traceId)
    {
        if (!SpanDictionary.TryGetValue(System.Text.Encoding.UTF8.GetString(traceId), out var record))
            return Enumerable.Empty<SpanRecord>();
        
        return record;
    }
}

internal interface ITraceRepository
{
    void AddSpans(ExportTraceServiceRequest request);
    IEnumerable<SpanRecord> GetTrace(byte[] traceId);
}

internal class SpanRecord
{
    public List<Span> Spans { get; set; } = new();
    public Resource Resource { get; set; } = new();
}

