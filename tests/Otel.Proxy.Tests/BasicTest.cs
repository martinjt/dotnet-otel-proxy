using System.Diagnostics;
using System.Net;
using Otel.Proxy.Tests.Setup;
using Otel.Proxy.Tests.TraceGenerators;
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
        var trace = new TraceModel
        {
            RootSpan = new SpanModel(),
            ChildSpans = { new SpanModel() }
        };

        var result = await Api.PostTracesAsExportRequest(trace);

        result.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SingleRootSpan_IsForwardedToHoneycomb()
    {
        var trace = new TraceModel
        {
            RootSpan = new SpanModel()
        };

        await Api.PostTracesAsExportRequest(trace);

        var exportedData = RecordedExportRequests.First();

        exportedData.ShouldNotBeNull();
        exportedData.ResourceSpans
            .First()
            .ScopeSpans.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SingleChildSpan_DoesNotSend()
    {
        var trace = new TraceModel
        {
            ChildSpans = { new SpanModel() }
        };

        await Api.PostTracesAsExportRequest(trace);

        RecordedExportRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SingleTraceWithAcrossTwoRequests_RootSpanInSecondRequest_ShouldSendBothSpansOnSecondRequest()
    {
        var traceId = ActivityTraceId.CreateRandom();

        var rootSpanOnlyModel = new TraceModel
        {
            TraceId = traceId,
            RootSpan = new SpanModel()
        };
        var childSpanOnlyModel = new TraceModel
        {
            TraceId = traceId,
            ChildSpans = {
                new SpanModel {
                    ParentSpanId = rootSpanOnlyModel.RootSpan.SpanId
                }
            }
        };

        await Api.PostTracesAsExportRequest(childSpanOnlyModel);

        await Api.PostTracesAsExportRequest(rootSpanOnlyModel);

        RecordedExportRequests.ShouldNotBeEmpty();
        RecordedExportRequests.ShouldHaveSingleItem();
        var exportedData = RecordedExportRequests.First();
        exportedData.ResourceSpans.Count.ShouldBe(2);
    }

    [Fact]
    public async Task RootSpanInSecondRequestForUnrelatedTrace_ShouldNotSendUnrelatedSpan()
    {
        var firstTraceWithOnlyChildSpan = new TraceModel
        {
            ChildSpans = { new SpanModel() }
        };

        var secondTraceWithRootSpan = new TraceModel
        {
            RootSpan = new SpanModel()
        };

        await Api.PostTracesAsExportRequest(firstTraceWithOnlyChildSpan);
        await Api.PostTracesAsExportRequest(secondTraceWithRootSpan);

        RecordedExportRequests.ShouldNotBeEmpty();
        RecordedExportRequests.ShouldHaveSingleItem();
        var exportedData = RecordedExportRequests.First();
        exportedData.ResourceSpans.Count.ShouldBe(1);
    }


}
