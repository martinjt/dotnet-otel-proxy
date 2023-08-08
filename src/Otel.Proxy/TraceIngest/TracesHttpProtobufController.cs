using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Otel.Proxy.Controllers;

internal class TracesHttpProtobufController : Controller
{
    private readonly ITraceRepository _traceRepository;
    private readonly ITraceProcessor _traceProcessor;

    public TracesHttpProtobufController(ITraceRepository traceRepository, ITraceProcessor traceProcessor)
    {
        _traceRepository = traceRepository;
        _traceProcessor = traceProcessor;
    }

    [HttpPost("/v1/traces")]
    public async Task<IResult> PostTrace([FromBody]ExportTraceServiceRequest exportRequest)
    {
        await _traceRepository.AddSpans(exportRequest);
        foreach (var traceId in exportRequest.ResourceSpans.SelectMany(
            rs => rs.ScopeSpans.SelectMany(
                ss => ss.Spans
                .Where(span => span.ParentSpanId.IsEmpty)
                .Select(s => s.TraceId)
            )))
            {
                await _traceProcessor.ProcessTrace(traceId.Memory.ToArray());
            }
        
        return Results.Accepted();
    }
}