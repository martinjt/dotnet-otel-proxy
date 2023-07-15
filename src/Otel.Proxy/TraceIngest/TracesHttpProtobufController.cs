using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Otel.Proxy.TraceRepository;

namespace Otel.Proxy.Controllers;

internal class TracesHttpProtobufController : Controller
{
    private readonly ITraceRepository _traceRepository;
    private readonly TraceProcessor _traceProcessor;

    public TracesHttpProtobufController(ITraceRepository traceRepository, TraceProcessor traceProcessor)
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