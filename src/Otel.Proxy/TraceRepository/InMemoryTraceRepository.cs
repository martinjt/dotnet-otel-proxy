using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Otel.Proxy.TraceRepository;

internal class InMemoryTraceRepository : ITraceRepository
{
    private readonly Func<InMemoryTraceStore> _traceStore;

    public InMemoryTraceRepository(Func<InMemoryTraceStore> traceStore)
    {
        _traceStore = traceStore;
    }

    public Task AddSpans(ExportTraceServiceRequest request)
    {
        return _traceStore().AddSpans(request);
    }

    public Task DeleteTrace(byte[] traceId)
    {
        return _traceStore().DeleteTrace(traceId);
    }

    public Task<IEnumerable<SpanRecord>> GetTrace(byte[] traceId)
    {
        return _traceStore().GetTrace(traceId);
    }
}

