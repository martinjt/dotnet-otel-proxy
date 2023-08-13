using Grpc.Core;
using OpenTelemetry.Proto.Collector.Trace.V1;
using static OpenTelemetry.Proto.Collector.Trace.V1.TraceService;
using Otel.Proxy.TraceRepository;


internal class TraceGrpcService : TraceServiceBase
{
    private readonly ITraceRepository _traceRepository;
    private readonly ITraceProcessor _traceProcessor;

    public TraceGrpcService(ITraceRepository traceRepository, ITraceProcessor traceProcessor)
    {
        _traceRepository = traceRepository;
        _traceProcessor = traceProcessor;
    }

    public override async Task<ExportTraceServiceResponse> Export(
        ExportTraceServiceRequest request,
        ServerCallContext context)
    {
        await _traceRepository.AddSpans(request);
        foreach (var traceId in request.ResourceSpans.SelectMany(
            rs => rs.ScopeSpans.SelectMany(
                ss => ss.Spans
                .Where(span => span.ParentSpanId.IsEmpty)
                .Select(s => s.TraceId)
            )))
            {
                await _traceProcessor.ProcessTrace(traceId.Memory.ToArray());
            }

        return new ExportTraceServiceResponse();
    }
}
