using System.Net;
using Otel.Proxy.Tests.Setup;
using Shouldly;

namespace Otel.Proxy.Tests;

public class SuccessTests
{
    private readonly OtelProxyAppFactory _server = new OtelProxyAppFactory();
    private HttpClient _api => _server.CreateClient();


    [Fact]
    public async Task SingleSpan_Returns204()
    {
        var exportRequest = TraceGenerator.CreateValidTraceExport();

        var result = await _api.PostExportRequest(exportRequest);

        result.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SingleSpan_SpanForwardedToHoneycomb()
    {
        var serviceName = Guid.NewGuid().ToString();
        var exportRequest = TraceGenerator.CreateValidTraceExport(serviceName);

        var result = await _api.PostExportRequest(exportRequest);

        var exportedData = _server.ReceivedExportRequests.First();

        exportedData.ShouldNotBeNull();
        exportedData.ResourceSpans
            .First()
            .Resource.Attributes
            .FirstOrDefault(a => a.Key == "service.name")?
            .Value.StringValue.ShouldBe(serviceName);
    }

    
}