using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using Otel.Proxy.TraceRepository;

namespace Otel.Proxy.Processing;

internal class NoSamplingTraceProcessor : ITraceProcessor
{
    private readonly ITraceRepository _traceRepository;
    private readonly HoneycombExporter _honeycombExporter;

    public NoSamplingTraceProcessor(
        ITraceRepository traceRepository,
        HoneycombExporter honeycombExporter)
    {
        _traceRepository = traceRepository;
        _honeycombExporter = honeycombExporter;
    }

    public async Task ProcessTrace(byte[] traceId)
    {
        var trace = await _traceRepository.GetTrace(traceId);
        
        var exportRequest = new ExportTraceServiceRequest();
        exportRequest.ResourceSpans.AddRange(
            trace.Select(rs => {
                var resourceSpan = new ResourceSpans() {
                    Resource = rs.Resource
                };
                resourceSpan.ScopeSpans.AddRange(
                    rs.Spans.Select(s => {
                        var scopeSpans = new ScopeSpans();
                        scopeSpans.Spans.AddRange(rs.Spans);
                        return scopeSpans;
                    }));
                return resourceSpan;
            })
        );

        await _honeycombExporter.SendRequestToHoneycomb(exportRequest);
    }

}

internal interface ITraceProcessor
{
    Task ProcessTrace(byte[] traceId);
}