using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Otel.Proxy.Setup;

namespace Otel.Proxy.Controllers;

internal class TracesHttpProtobufController : Controller
{
    private readonly ITraceRepository _traceRepository;
    private readonly IProcessScheduler _processScheduler;
    private readonly IOptions<ProcessingSettings> _processingSettings;

    public TracesHttpProtobufController(ITraceRepository traceRepository, IProcessScheduler processScheduler, 
        IOptions<ProcessingSettings> processingSettings)
    {
        _traceRepository = traceRepository;
        _processScheduler = processScheduler;
        _processingSettings = processingSettings;
    }

    [HttpPost("/v1/traces")]
    public async Task<IResult> PostTrace([FromBody]ExportTraceServiceRequest exportRequest)
    {
        await _traceRepository.AddSpans(exportRequest);

        var traceIds = new Dictionary<byte[], bool>();
        foreach (var span in exportRequest.ResourceSpans.SelectMany(rs => rs.ScopeSpans.SelectMany(ils => ils.Spans)))
        {
            var traceId = Convert.ToHexString(span.TraceId.ToByteArray());
            var isRoot = span.ParentSpanId.IsEmpty || span.ParentSpanId == ByteString.Empty;
            if (!traceIds.ContainsKey(span.TraceId.ToByteArray()))
            {
                traceIds.Add(span.TraceId.ToByteArray(), isRoot);
            }
            else if (isRoot && !traceIds[span.TraceId.ToByteArray()])
            {
                traceIds[span.TraceId.ToByteArray()] = isRoot;
            }
        }

        foreach (var kv in traceIds)
        {
            var delay = kv.Value ? _processingSettings.Value.RootSpanProcessingDelayInSeconds : 
                _processingSettings.Value.TraceProcessingTimeoutInSeconds;
            await _processScheduler.ScheduleTraceProcessing(kv.Key, delay);
        }
        
        HttpContext.Response.ContentType = "application/x-protobuf";
        return Results.Accepted();
    }
}