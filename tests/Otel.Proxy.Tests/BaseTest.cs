using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Trace;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Otel.Proxy.Tests;

[UpdateActivityWithTestName]
[Collection(OTelCollection.Name)]
public abstract class BaseTest : IAsyncLifetime
{
    public static readonly ActivitySource Source = new("Tests");
    internal readonly HttpClient Api;
    private readonly Activity? _testActivity;
    internal readonly OTelFixture _oTelFixture;
    private readonly ITestOutputHelper _output;

    public BaseTest(OTelFixture oTelFixture, ITestOutputHelper output)
    {
        _oTelFixture = oTelFixture;
        _output = output;
        _testActivity = Source.StartActivity("Test Started");
        
        Api = _oTelFixture.Server.CreateHTTPClient();
        if (_testActivity != null)
            Api.DefaultRequestHeaders.Add("traceparent", 
            $"00-{_testActivity.TraceId}-{_testActivity.SpanId}-01");
    }

    public Task InitializeAsync()
    {
        _oTelFixture.TracerProvider?.ForceFlush();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _oTelFixture.WriteTraceLinkToOutput(_output, _testActivity!);
        _testActivity?.Stop();
        _oTelFixture.TracerProvider?.ForceFlush();
        return Task.CompletedTask;
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
