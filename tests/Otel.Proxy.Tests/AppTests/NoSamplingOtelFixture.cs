using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Otel.Proxy.Interfaces;
using Otel.Proxy.Tests.Setup;
using Xunit.Abstractions;

namespace Otel.Proxy.Tests.AppTests;

[CollectionDefinition(Name)]
public class NoSamplingCollection : ICollectionFixture<NoSamplingOtelFixture>
{
    public const string Name = "OtelCollection";
}

public class NoSamplingOtelFixture : OTelFixture
{
    public NoSamplingOtelFixture() 
        : base(new List<ISampler>())
    {
    }
}

public class OTelFixture
{
    public readonly OtelProxyAppFactory Server;
    public readonly HoneycombTestOutputHelper LinkOutputWriter;
    private const string ServiceName = "otel-proxy-tests";

    public static TracerProvider? TracerProvider = Sdk.CreateTracerProviderBuilder()
        .ConfigureResource(r => r
            .AddService(ServiceName)
            .AddAttributes(new List<KeyValuePair<string, object>>
                {
                    new("test.run_id", Guid.NewGuid().ToString("N")),
                    new("test.start_time", DateTime.UtcNow.ToString("O")),
                    new("test.start_ticks", DateTime.UtcNow.Ticks)
                }))
        .AddSource(ActivityWrappedBaseTest.Source.Name)
        .SetSampler<AlwaysOnSampler>()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri($"https://api.honeycomb.io:443");
            otlpOptions.Headers = string.Join(",", new List<string>
            {
                "x-otlp-version=0.17.0",
                $"x-honeycomb-team={Environment.GetEnvironmentVariable("HONEYCOMB_API_KEY")}"
            });
        })
        .Build();

    public OTelFixture(List<ISampler> samplers = null!)
    {
        Server = new OtelProxyAppFactory(TracerProvider, samplers);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build();
        LinkOutputWriter = new HoneycombTestOutputHelper(configuration, ServiceName);
    }

    public void WriteTraceLinkToOutput(ITestOutputHelper outputHelper, Activity activity)
    {
        LinkOutputWriter.RecordTest(outputHelper, activity);    
    }

}