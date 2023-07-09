using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;


namespace Otel.Proxy.TraceRepository;

internal interface ITraceRepository
{
    Task AddSpans(ExportTraceServiceRequest request);
    Task<IEnumerable<SpanRecord>> GetTrace(byte[] traceId);
}

internal class SpanRecord
{
    public List<Span> Spans { get; set; } = new();
    public Resource Resource { get; set; } = new();
}

