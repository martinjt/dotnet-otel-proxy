using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Trace.V1;
using ProtoBuf;

public class TracesController : Controller
{
    private readonly ILogger<TracesController> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public TracesController(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    [HttpPost("/v1/traces")]
    public async Task<IResult> PostTrace([FromBody]ExportTraceServiceRequest exportRequest)
    {
        using var ms = new MemoryStream();

        var content = new StreamContent(ms);
        Serializer.Serialize(ms, exportRequest);
        ms.Seek(0, SeekOrigin.Begin);

        content.Headers.ContentType = new MediaTypeHeaderValue(Request.ContentType!);
        content.Headers.Add("x-honeycomb-team", _configuration["HoneycombApiKey"]);
        var result = await _httpClient.PostAsync("https://api.honeycomb.io/v1/traces", content);

        return Results.Ok();
    }
}