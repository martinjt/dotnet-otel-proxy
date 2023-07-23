using System.Diagnostics;
using System.Net;
using Otel.Proxy.Tests.Setup;
using Shouldly;
using Xunit.Abstractions;

namespace Otel.Proxy.Tests;


public class SuccessTests : BaseTest
{
    public SuccessTests(OTelFixture fixture, ITestOutputHelper testOutputHelper) 
        : base(fixture, testOutputHelper)
    {

    }

    [Fact]
    public async Task SingleSpan_Returns204()
    {
        var exportRequest = TraceGenerator.CreateValidTraceExport();

        var result = await Api.PostExportRequest(exportRequest);

        result.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SingleRootSpan_IsForwardedToHoneycomb()
    {
        var serviceName = Guid.NewGuid().ToString();
        var exportRequest = new ExportServiceRequestBuilder()
            .WithService(serviceName)
            .WithTrace(o => o
                .WithRootSpan().ForService(serviceName))
            .Build();

        var result = await Api.PostExportRequest(exportRequest);

        var exportedData = RecordedExportRequests.First();

        exportedData.ShouldNotBeNull();
        exportedData.ResourceSpans
            .First()
            .ScopeSpans.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SingleChildSpan_DoesNotSend()
    {
        var serviceName = "does-not-matter";
        var parentSpanIdThatHasNotBeenSent = ActivitySpanId.CreateRandom();

        var exportRequest = new ExportServiceRequestBuilder()
            .WithService(serviceName)
            .WithTrace(trace =>
                trace.WithSpan(span =>
                    span.ForService(serviceName),
                        parentSpanId: parentSpanIdThatHasNotBeenSent
                    )
            ).Build();

        var result = await Api.PostExportRequest(exportRequest);

        RecordedExportRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SingleTraceWithAcrossTwoRequests_RootSpanInSecondRequest_ShouldSendBothSpansOnSecondRequest()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var rootSpanId = ActivitySpanId.CreateRandom();
        var serviceName = "does-not-matter";

        var rootSpanOnlyRequest = new ExportServiceRequestBuilder()
            .WithService(serviceName)
            .WithTrace(traceId, trace =>
                trace.WithSpan(span =>
                    span
                        .ForService(serviceName),
                        spanId: rootSpanId // THIS IS IMPORTANT
                    )
            ).Build();

        var childSpanOnlyRequest = new ExportServiceRequestBuilder()
            .WithService(serviceName)
            .WithTrace(traceId, trace =>
                trace.WithSpan(span =>
                    span
                        .ForService(serviceName),
                        parentSpanId: rootSpanId // THIS IS IMPORTANT
                    )
            ).Build();

        await Api.PostExportRequest(childSpanOnlyRequest);

        await Api.PostExportRequest(rootSpanOnlyRequest);

        RecordedExportRequests.ShouldNotBeEmpty();
        RecordedExportRequests.ShouldHaveSingleItem();
        var exportedData = RecordedExportRequests.First();
        exportedData.ResourceSpans.Count().ShouldBe(2);
    }

    [Fact]
    public async Task RootSpanInSecondRequestForUnrelatedTrace_ShouldNotSendUnrelatedSpan()
    {
        var childSpanOnlyRequest = new ExportServiceRequestBuilder()
            .WithService("service1")
            .WithTrace(trace => 
                trace.WithSpan(span => 
                    span.WithAttribute("myattribute", "myvalue")
                        .ForService("service1"),
                        parentSpanId: ActivitySpanId.CreateRandom()
                    )
            ).Build();

        await Api.PostExportRequest(childSpanOnlyRequest);

        var rootSpanOnlyRequest = new ExportServiceRequestBuilder()
            .WithService("service1")
            .WithTrace( trace => 
                trace.WithRootSpan(span => 
                    span.WithAttribute("myattribute", "myvalue")
                        .ForService("service1")
                    )
            ).Build();

        await Api.PostExportRequest(rootSpanOnlyRequest);
        RecordedExportRequests.ShouldNotBeEmpty();
        RecordedExportRequests.ShouldHaveSingleItem();
        var exportedData = RecordedExportRequests.First();
        exportedData.ResourceSpans.Count().ShouldBe(1);
    }


}
