using System.Collections.Concurrent;
using OpenTelemetry.Proto.Collector.Trace.V1;

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
                var traceId = Convert.ToHexString(grouping.Key.ToByteArray());
                var record = new SpanRecord
                {
                    Spans = grouping.ToList(),
                    Resource = resourceSpan.Resource
                };
                var records = SpanDictionary.GetOrAdd(traceId,
                    (b) => []);
                records.Add(record);
            }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<SpanRecord>> GetTrace(byte[] traceId)
    {
        if (!SpanDictionary.TryGetValue(Convert.ToHexString(traceId), out var record))
            return Task.FromResult(Enumerable.Empty<SpanRecord>());
        
        return Task.FromResult(record as IEnumerable<SpanRecord>);
    }

    internal Task DeleteTrace(byte[] traceId)
    {
        SpanDictionary.TryRemove(Convert.ToHexString(traceId), out _);
        return Task.CompletedTask;
    }
}
