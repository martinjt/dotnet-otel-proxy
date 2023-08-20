using System.Net.NetworkInformation;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Otel.Proxy.Interfaces;
using Otel.Proxy.Tests.Setup;
using Otel.Proxy.Tests.TraceGenerators;
using Xunit.Abstractions;

namespace Otel.Proxy.Tests;

public class SamplerTests : ActivityWrappedBaseTest
{
    private readonly OTelFixture otel;
    private readonly HttpClient Api;
    private const string SampleRuleKey = "meta.sample.reason";
    private const string SampleRateAttributeName = "SampleRate";
    private List<ExportTraceServiceRequest> RecordedExportRequests 
        => otel.Server.ReceivedExportRequests;

    private static readonly ConsistentRateSampler _keep500Sampler = 
        new ConsistentRateSampler("Keep 500s", new List<SampleCondition> {
            new ("http.status", 500, ConditionsOperator.Equals)
        }, 1);

    private AverageRateSampler _sampleAtOneInFive =>
        new AverageRateSampler(new InMemoryAverageRateSamplerStore(20), "Sample at 1 in 5", 5, new HashSet<string> {
            "http.status", "http.method"
        });

    private List<ISampler> _defaultSamplers => new() {
        _keep500Sampler,
        _sampleAtOneInFive
    };

    public SamplerTests(ITestOutputHelper output) 
    {
        otel = new OTelFixture(_defaultSamplers);
        Api = otel.Server.CreateHTTPClient();
        SetTraceHeadersOnHttpClient(Api);
    }

    [Fact]
    public async Task SampledTrace_AllSpansShouldHaveSamplerNameAsAttribute()
    {
        var trace = new TraceModel
        {
            RootSpan = new SpanModel {
                Attributes = { {"http.status", 500} }
            },
            ChildSpans = { new SpanModel() }
        };

        await Api.PostTracesAsExportRequest(trace);

        var exportedData = RecordedExportRequests.First();
    
        exportedData.GetAllSpansAsList()
            .AllSpansShouldHaveAttribute(SampleRuleKey, _keep500Sampler.Name);
    }

    [Fact]
    public async Task SampledTrace_AllSpansShouldHaveSamplerRateAsAttribute()
    {
        var trace = new TraceModel
        {
            RootSpan = new SpanModel {
                Attributes = { {"http.status", 500} }
            },
            ChildSpans = { new SpanModel() }
        };

        await Api.PostTracesAsExportRequest(trace);

        var exportedData = RecordedExportRequests.First();
    
        exportedData.GetAllSpansAsList()
            .AllSpansShouldHaveAttribute(SampleRateAttributeName, 1);
    }
}