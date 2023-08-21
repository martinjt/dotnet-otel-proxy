using System.Diagnostics;
using OpenTelemetry.Trace;
using Otel.Proxy.Tests.AppTests;
using Xunit.Sdk;

namespace Otel.Proxy.Tests.Setup;

public abstract class ActivityWrappedBaseTest : IAsyncLifetime
{
    public static readonly ActivitySource Source = new("Tests");
    protected readonly Activity? _testActivity;
    private readonly TracerProvider _ensureTracerProviderIsBuilt = OTelFixture.TracerProvider!;
    public ActivityWrappedBaseTest()
    {
        if (_ensureTracerProviderIsBuilt == null)
            throw new XunitException("TracerProvider is null");

        _testActivity = Source.StartActivity("Test Started");
    }

    public Task InitializeAsync()
    {
        OTelFixture.TracerProvider?.ForceFlush();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _testActivity?.Stop();
        OTelFixture.TracerProvider?.ForceFlush();
        return Task.CompletedTask;
    }
    internal void SetTraceHeadersOnHttpClient(HttpClient client)
    {
        if (_testActivity != null)
            client.DefaultRequestHeaders.Add("traceparent", 
            $"00-{_testActivity.TraceId}-{_testActivity.SpanId}-01");
    }
}
