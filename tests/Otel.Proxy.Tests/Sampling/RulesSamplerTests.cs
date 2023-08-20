using Otel.Proxy.Interfaces;

namespace Otel.Proxy.Tests.Sampling;

public class RulesSamplerTests
{
    private readonly List<KeyValuePair<string, object>> _defaultTagList = new()  {
         { new("service.name", "my-service-name") },
         { new("http.url", "https://localhost:5001/WeatherForecast") },
         { new("http.method", "GET") },
         { new("http.host", "localhost:5001") },
         { new("http.scheme", "https") },
         { new("http.target", "/WeatherForecast") },
         { new("http.flavor", "2.0") },
         { new("http.status_code", 200) },
         { new("http.status_text", "OK") },
         { new("http.user_agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0") },
         { new("http.request_content_length", 0) },
         { new("http.response_content_length", 2) },
         { new("http.route", "/WeatherForecast") },
         { new("http.client_ip", "::1") },
         { new("http.server_ip", "::1") },
         { new("http.port", 5001) },
         { new("http.host_port", "localhost:5001") },
         { new("http.target_port", 5001) },
    };

    [Fact]
    public async Task SamplerWithoutConditions_ReturnsTrue()
    {
        var sut = new ConsistentRateSampler("dummy", Enumerable.Empty<SampleCondition>(), 11);

        var sampleDecision = await sut.ShouldSample(_defaultTagList);

        Assert.True(sampleDecision);
    }

    [Fact]
    public async Task SamplerWithOneEqualsCondition_WithMatchingTagValue_ReturnsTrue()
    {
        var sut = new ConsistentRateSampler("dummy",
            new[] { new SampleCondition("service.name", "my-service-name", ConditionsOperator.EqualTo)
        }, 11);

        var sampleDecision =  await sut.ShouldSample(_defaultTagList);

        Assert.True(sampleDecision);
    }

    [Fact]
    public async Task SamplerWithOneEqualsCondition_WithMatchingTagWithIncorrectValue_ReturnsFalse()
    {
        var sut = new ConsistentRateSampler("dummy", new[] { 
            new SampleCondition("service.name", "non-matching-service-name", ConditionsOperator.EqualTo)
        }, 11);

        var sampleDecision =  await sut.ShouldSample(_defaultTagList);

        Assert.False(sampleDecision);
    }
}