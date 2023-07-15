using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Trace;
using Otel.Proxy.Tests.Setup;
using Xunit.Sdk;

namespace Otel.Proxy.Tests;

[UpdateActivityWithTestName]
public abstract class BaseTest : IAsyncLifetime
{
    private readonly OtelProxyAppFactory _server;
    public static readonly ActivitySource Source = new("Tests");
    internal readonly HttpClient Api;
    private readonly Activity? _testActivity;

    public BaseTest()
    {
        _server = new OtelProxyAppFactory();
        Api = _server.CreateHTTPClient();
        _testActivity = Source.StartActivity("Test Started");
        if (_testActivity != null)
            Api.DefaultRequestHeaders.Add("traceparent", 
            $"00-{_testActivity.TraceId}-{_testActivity.SpanId}-01");
    }

    public Task InitializeAsync()
    {
        OtelProxyAppFactory.TracerProvider?.ForceFlush();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _testActivity?.Stop();
        OtelProxyAppFactory.TracerProvider?.ForceFlush();
        return Task.CompletedTask;
    }

    internal IEnumerable<ExportTraceServiceRequest> RecordedExportRequests => _server.ReceivedExportRequests;
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
