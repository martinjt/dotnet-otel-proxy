using System.Net.Http.Headers;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using Otel.Proxy.TraceRepository;

internal class TraceProcessor
{
    private readonly ITraceRepository _traceRepository;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public TraceProcessor(
        ITraceRepository traceRepository,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _traceRepository = traceRepository;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task ProcessTrace(byte[] traceId)
    {
        var trace = await _traceRepository.GetTrace(traceId);
        
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
        var content = new ByteArrayContent(exportRequest.ToByteArray());

        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        content.Headers.Add("x-honeycomb-team", _configuration["HoneycombApiKey"]);
        await _httpClient.PostAsync("https://api.honeycomb.io/v1/traces", 
            new ByteArrayContent(exportRequest.ToByteArray()));
    }
}