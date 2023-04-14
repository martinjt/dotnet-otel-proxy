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
        var exportRequest = new ExportServiceRequestBuilder()
            .WithService(serviceName)
            .WithTrace(o => o
                .WithRootSpan().ForService(serviceName))
            .Build();

        var result = await _api.PostExportRequest(exportRequest);

        var exportedData = _server.ReceivedExportRequests.First();

        exportedData.ShouldNotBeNull();
        exportedData.ResourceSpans
            .First()
            .ScopeSpans.ShouldHaveSingleItem();
    }

    public async Task SingleSpan_DoesNotSendIfNoRootSpan()
    {
        var requestBuilder = new ExportServiceRequestBuilder()
            .WithService("service1")
            .WithService("service2")
            .WithTrace(trace => 
                trace.WithRootSpan(rootSpan => 
                    rootSpan.WithChildSpan(o => 
                        o
                        .WithAttribute("myattribute", "myvalue")
                        .ForService("service1")
                    )
                    .ForService("test-service")
            ));
    }
    
}