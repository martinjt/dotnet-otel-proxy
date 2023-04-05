using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Trace.V1;
using ProtoBuf;

public class TracesController : Controller
{
    private readonly TraceRepository _traceRepository;
    private readonly TraceProcessor _traceProcessor;

    public TracesController(TraceRepository traceRepository, TraceProcessor traceProcessor)
    {
        _traceRepository = traceRepository;
        _traceProcessor = traceProcessor;
    }

    [HttpPost("/v1/traces")]
    public async Task<IResult> PostTrace([FromBody]ExportTraceServiceRequest exportRequest)
    {
        _traceRepository.AddSpans(exportRequest);
        foreach (var traceId in exportRequest.ResourceSpans.SelectMany(
            rs => rs.ScopeSpans.SelectMany(
                ss => ss.Spans.Select(s => s.TraceId)
            )))
            {
                await _traceProcessor.ProcessTrace(traceId);
            }
        
        return Results.Accepted();
    }
}