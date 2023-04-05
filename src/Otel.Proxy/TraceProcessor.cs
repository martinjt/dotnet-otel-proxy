using System.Net.Http.Headers;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using ProtoBuf;

public class TraceProcessor
{
    private readonly TraceRepository _traceRepository;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public TraceProcessor(
        TraceRepository traceRepository,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _traceRepository = traceRepository;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task ProcessTrace(byte[] traceId)
    {
        var trace = _traceRepository.GetTrace(traceId);
        
        var exportRequest = new ExportTraceServiceRequest();
        exportRequest.ResourceSpans.AddRange(
            trace.Select(rs => {
                var resourceSpan = new ResourceSpans();
                resourceSpan.Resource = rs.Resource;
                resourceSpan.ScopeSpans.AddRange(
                    rs.Spans.Select(s => {
                        var scopeSpans = new ScopeSpans();
                        scopeSpans.Spans.AddRange(rs.Spans);
                        return scopeSpans;
                    }));
                return resourceSpan;
            })
        );

        await SendRequestToHoneycomb(exportRequest);
    }

    private async Task SendRequestToHoneycomb(ExportTraceServiceRequest exportRequest)
    {
        using var ms = new MemoryStream();

        var content = new StreamContent(ms);
        Serializer.Serialize(ms, exportRequest);
        ms.Seek(0, SeekOrigin.Begin);

        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        content.Headers.Add("x-honeycomb-team", _configuration["HoneycombApiKey"]);
        await _httpClient.PostAsync("https://api.honeycomb.io/v1/traces", content);
    }
}