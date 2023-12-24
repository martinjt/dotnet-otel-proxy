using OpenTelemetry.Proto.Collector.Trace.V1;
using Otel.Proxy.Tests.Extensions;
using Otel.Proxy.Tests.Setup;
using Otel.Proxy.Tests.TraceGenerators;
using Xunit.Abstractions;

namespace Otel.Proxy.Tests.AppTests.Sampling;

public class DryRunSamplerTests : ActivityWrappedBaseTest, IClassFixture<SamplingClassFixture>
{
    private readonly HttpClient Api;
    private readonly SamplingClassFixture _fixture;
    private const string SampleRuleKey = "meta.sample.reason";
    private const string SampleRateAttributeName = "SampleRate";
    private List<ExportTraceServiceRequest> RecordedExportRequests 
        => _fixture.Server.ReceivedExportRequests;

    public DryRunSamplerTests(SamplingClassFixture fixture, ITestOutputHelper output) 
    {
        _fixture = fixture;
        Api = _fixture.Server.CreateHTTPClient();
        SetTraceHeadersOnHttpClient(Api);
    }

    [Fact]
    public async Task TraceWithAttributesThatDontMatchFirstSamplerCondition_UsesNextSampler()
    {
        var nonMatchingStatusCode = 200;
        var traceWhichDoesntMatchSamplingRules = new TraceModel
        {
            RootSpan = new SpanModel {
                Attributes = { {"http.status", nonMatchingStatusCode} }
            },
            ChildSpans = { new SpanModel() }
        };

        await Api.PostTracesAsExportRequest(traceWhichDoesntMatchSamplingRules);

        var exportedData = RecordedExportRequests.First();
    
        exportedData.GetAllSpansAsList()
            .AllSpansShouldHaveAttribute(SampleRuleKey, _fixture.SampleAllAtOneInFiveFor200s.Name);
    }

    [Fact]
    public async Task TraceMatchingSamplingRule_AllSpansShouldHaveCorrespondingSamplerNameAsAttribute()
    {
        var traceWhichMatchingSamplingRule = new TraceModel
        {
            RootSpan = new SpanModel {
                Attributes = { 
                    _fixture
                    .Keep500StatusCodesSampler
                    .ConditionsAsDictionary()
                }
            },
            ChildSpans = { new SpanModel() }
        };

        await Api.PostTracesAsExportRequest(traceWhichMatchingSamplingRule);

        var exportedData = RecordedExportRequests.First();
    
        exportedData.GetAllSpansAsList()
            .AllSpansShouldHaveAttribute(SampleRuleKey, _fixture.Keep500StatusCodesSampler.Name);
    }

    [Fact]
    public async Task SampledTrace_AllSpansShouldHaveSamplerRateAsAttribute()
    {
        var traceWhichMatchingSamplingRule = new TraceModel
        {
            RootSpan = new SpanModel {
                Attributes = { 
                    _fixture
                    .Keep500StatusCodesSampler
                    .ConditionsAsDictionary()
                }
            },
            ChildSpans = { new SpanModel() }
        };

        await Api.PostTracesAsExportRequest(traceWhichMatchingSamplingRule);

        var exportedData = RecordedExportRequests.First();
    
        exportedData.GetAllSpansAsList()
            .AllSpansShouldHaveAttribute(SampleRateAttributeName, 1);
    }
}
