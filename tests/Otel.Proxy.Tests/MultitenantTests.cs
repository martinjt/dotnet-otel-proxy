using System.Diagnostics;
using Otel.Proxy.Tests.Setup;
using Shouldly;
using Xunit.Abstractions;

namespace Otel.Proxy.Tests;


public class MultitenantTests : BaseTest
{
    public MultitenantTests(OTelFixture fixture, ITestOutputHelper testOutputHelper) 
        : base(fixture, testOutputHelper)
    {

    }

    [Fact]
    public async Task RootSpanInSecondRequestForDifferentTenant_ShouldNotSendChildSpan()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var rootSpanId = ActivitySpanId.CreateRandom();
        var childSpanOnlyRequest = new ExportServiceRequestBuilder()
            .WithService("service1")
            .WithTrace(traceId, trace => 
                trace.WithSpan(span => 
                    span.WithAttribute("myattribute", "myvalue")
                        .ForService("service1"),
                        parentSpanId: rootSpanId
                    )
            ).Build();

        await Api.PostExportRequest(childSpanOnlyRequest);

        var rootSpanOnlyRequest = new ExportServiceRequestBuilder()
            .WithService("service1")
            .WithTrace(traceId, trace => 
                trace.WithSpan(span => 
                    span.WithAttribute("myattribute", "myvalue")
                        .ForService("service1"),
                        spanId: rootSpanId
                    )
            ).Build();

        await Api.PostExportRequest(rootSpanOnlyRequest, "NewTenant");
        RecordedExportRequests.ShouldNotBeEmpty();
        RecordedExportRequests.ShouldHaveSingleItem();
        var exportedData = RecordedExportRequests.First();
        exportedData.ResourceSpans.Count().ShouldBe(1);
    }
}
