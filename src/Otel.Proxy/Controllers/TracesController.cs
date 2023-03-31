using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Trace.V1;

public class TracesController : Controller
{
    private readonly ILogger<TracesController> _logger;

    public TracesController(ILogger<TracesController> logger)
    {
        _logger = logger;
    }

    [HttpPost("/v1/traces")]
    public IResult PostTrace([FromBody]ExportTraceServiceRequest exportRequest)
    {
        _logger.LogInformation("Called Export");
        _logger.LogInformation("MediaType {mediaType}", Request.ContentType);
        foreach (var rs in exportRequest.ResourceSpans)
        {
            _logger.LogInformation("Service Name: {serviceName}", rs
                .Resource
                .Attributes
                .First(a => a.Key == "service.name").Value);
            foreach (var span in rs.ScopeSpans.SelectMany(ss => ss.Spans))
            {
                _logger.LogInformation("SpanId: {spanId} TraceId: {traceId}", Convert.ToBase64String(span.SpanId), Convert.ToBase64String(span.TraceId));
            }
        };
       
        return Results.Ok();
    }
}