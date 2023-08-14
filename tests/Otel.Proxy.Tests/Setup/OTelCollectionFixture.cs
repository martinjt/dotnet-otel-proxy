using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Otel.Proxy.Tests;
using Otel.Proxy.Tests.Setup;
using Xunit.Abstractions;

[CollectionDefinition(Name)]
public class OTelCollection : ICollectionFixture<OTelFixture>
{
    public const string Name = "OtelCollection";
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
        .AddSource(BaseTest.Source.Name)
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

    public OTelFixture()
    {
        Server = new OtelProxyAppFactory(TracerProvider);
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