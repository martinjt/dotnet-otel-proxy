using Otel.Proxy.Interfaces;
using Otel.Proxy.Tests.Setup;

namespace Otel.Proxy.Tests.AppTests.Sampling;

public class SamplingClassFixture
{
    public ConsistentRateSampler Keep500StatusCodesSampler = 
        new("Keep 500s", new List<SampleCondition> {
            new ("http.status", 500, ConditionsOperator.EqualTo)
        }, 1);

    public AverageRateSampler SampleAllAtOneInFiveFor200s =>
        new(new InMemoryAverageRateSamplerStore(20), "Sample at 1 in 5 for 200s", 5, new HashSet<string> {
            "http.status", "http.method"
        }, new List<SampleCondition> {
            new ("http.status", 200, ConditionsOperator.EqualTo)
        });

    private List<ISampler> _defaultSamplers => new() {
        Keep500StatusCodesSampler,
        SampleAllAtOneInFiveFor200s
    };

    private OTelFixture Otel { get; }
    public OtelProxyAppFactory Server { get; }

    public SamplingClassFixture()
    {
        Otel = new OTelFixture(_defaultSamplers);
        Server = Otel.Server;
    }

}
