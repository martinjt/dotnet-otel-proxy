using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Trace;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Otel.Proxy.Tests;


public abstract class ActivityWrappedBaseTest : IAsyncLifetime
{
    public static readonly ActivitySource Source = new("Tests");
    protected readonly Activity? _testActivity;
    public ActivityWrappedBaseTest()
    {
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
[UpdateActivityWithTestName]
[Collection(NoSamplingCollection.Name)]
public abstract class BaseTest : ActivityWrappedBaseTest, IAsyncLifetime
{
    internal readonly HttpClient Api;
    internal readonly OTelFixture _oTelFixture;
    private readonly ITestOutputHelper _output;

    public BaseTest(OTelFixture oTelFixture, ITestOutputHelper output)
    {
        _oTelFixture = oTelFixture;
        _output = output;
        
        Api = _oTelFixture.Server.CreateHTTPClient();
        SetTraceHeadersOnHttpClient(Api);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        _oTelFixture.WriteTraceLinkToOutput(_output, _testActivity!);
    }

    internal IEnumerable<ExportTraceServiceRequest> RecordedExportRequests => _oTelFixture.Server.ReceivedExportRequests;
}

public class UpdateActivityWithTestName : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        var activity = Activity.Current;
        while (true)
        {
            if (activity == null)
                break;
            
            if (activity.Source.Name == BaseTest.Source.Name)
            {
                activity.DisplayName = $"Test: {methodUnderTest.Name}";
                break;
            }

            if (activity.Parent == null)
                break;

            activity = activity.Parent;
        }
    }

}
