using JustEat.HttpClientInterception;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Collector.Trace.V1;
using ProtoBuf;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using System.Net;

namespace Otel.Proxy.Tests.Setup;

public class OtelProxyAppFactory : WebApplicationFactory<Program>
{
    public List<ExportTraceServiceRequest> ReceivedExportRequests = new();
    private readonly HttpClientInterceptorOptions _httpClientInterceptorOptions = new();
    private static bool RegisteredExceptionHandler = false;

    public static TracerProvider? TracerProvider = Sdk.CreateTracerProviderBuilder()
        .ConfigureResource(r => r
            .AddService("Otel Proxy Tests")
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

    public OtelProxyAppFactory()
    {
        if (!RegisteredExceptionHandler)
            AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
            {
                if (args.Exception.Source == "Shouldly" ||
                    args.Exception.Source == "xunit.assert")
                    Activity.Current?.AddTag("test.outcome", "failed");
            };
        SetupInterceptor();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services => {
            services.AddLogging(l => l.ClearProviders());
            services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                _ => new HttpClientInterceptionFilter(_httpClientInterceptorOptions));
            if (TracerProvider != null)
                services.AddSingleton(TracerProvider);
        });

        base.ConfigureWebHost(builder);
    }

    public HttpClient CreateHTTPClient()
    {
        HttpClient client;
        var serverHandler = Server.CreateHandler(o => {
            o.Features.Set<IHttpConnectionFeature>(new HttpConnectionFeature
            {
                LocalIpAddress = IPAddress.Loopback,
                LocalPort = 4318
            });
        });

        client = new HttpClient(serverHandler);

        ConfigureClient(client);

        return client;
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
