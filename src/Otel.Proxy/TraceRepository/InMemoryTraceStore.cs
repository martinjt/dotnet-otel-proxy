using System.Collections.Concurrent;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Otel.Proxy.TraceRepository;

internal class InMemoryTraceStore
{
    private ConcurrentDictionary<string, ConcurrentBag<SpanRecord>> SpanDictionary = new();

    public Task AddSpans(ExportTraceServiceRequest request)
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

        return Task.CompletedTask;
    }

    public Task<IEnumerable<SpanRecord>> GetTrace(byte[] traceId)
    {
        if (!SpanDictionary.TryGetValue(System.Text.Encoding.UTF8.GetString(traceId), out var record))
            return Task.FromResult(Enumerable.Empty<SpanRecord>());
        
        return Task.FromResult(record as IEnumerable<SpanRecord>);
    }

    internal Task DeleteTrace(byte[] traceId)
    {
        SpanDictionary.TryRemove(System.Text.Encoding.UTF8.GetString(traceId), out _);
        return Task.CompletedTask;
    }
}
