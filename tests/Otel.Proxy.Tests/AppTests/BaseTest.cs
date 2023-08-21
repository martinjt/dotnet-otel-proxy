using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Otel.Proxy.Tests.Setup;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Otel.Proxy.Tests.AppTests;
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
