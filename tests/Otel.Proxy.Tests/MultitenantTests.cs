using System.Diagnostics;
using Otel.Proxy.Tests.Setup;
using Otel.Proxy.Tests.TraceGenerators;
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
        await Api.PostTracesAsExportRequest(rootSpanOnlyModel, "NewTenant");

        RecordedExportRequests.ShouldNotBeEmpty();
        RecordedExportRequests.ShouldHaveSingleItem();
        var exportedData = RecordedExportRequests.First();
        exportedData.ResourceSpans.Count().ShouldBe(1);
    }
}
