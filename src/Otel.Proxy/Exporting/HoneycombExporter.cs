using System.Net.Http.Headers;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Trace.V1;

internal class HoneycombExporter
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public HoneycombExporter(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task SendRequestToHoneycomb(ExportTraceServiceRequest exportRequest)
    {
        var content = new ByteArrayContent(exportRequest.ToByteArray());

        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        content.Headers.Add("x-honeycomb-team", _configuration["HoneycombApiKey"]);
        await _httpClient.PostAsync("https://api.honeycomb.io/v1/traces", 
            new ByteArrayContent(exportRequest.ToByteArray()));
    }
}