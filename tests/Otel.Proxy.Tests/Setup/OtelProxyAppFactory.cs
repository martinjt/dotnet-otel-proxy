using JustEat.HttpClientInterception;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Collector.Trace.V1;
using ProtoBuf;

namespace Otel.Proxy.Tests.Setup;

public class OtelProxyAppFactory : WebApplicationFactory<Program>
{
    public List<ExportTraceServiceRequest> ReceivedExportRequests = new();
    private readonly HttpClientInterceptorOptions _httpClientInterceptorOptions = new();

    public OtelProxyAppFactory()
    {
        SetupInterceptor();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services => {
            services.AddLogging(l => l.ClearProviders());
            services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                _ => new HttpClientInterceptionFilter(_httpClientInterceptorOptions));
        });
        base.ConfigureWebHost(builder);
    }

    private void SetupInterceptor()
    {
        var interceptionBuilder = new HttpRequestInterceptionBuilder();

        new HttpRequestInterceptionBuilder()
            .Requests()
            .ForPost()
            .ForHost("api.honeycomb.io")
            .ForHttps()
            .ForPath("v1/traces")
            .Responds()
            .WithStatus(200)
            .WithInterceptionCallback(message => {
                if (message.Content != null)
                {
                    var exportRequest = Serializer.Deserialize<ExportTraceServiceRequest>(message.Content.ReadAsStream());
                    ReceivedExportRequests.Add(exportRequest);
                }
            })
            .RegisterWith(_httpClientInterceptorOptions);
    }
}
