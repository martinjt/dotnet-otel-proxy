using JustEat.HttpClientInterception;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Collector.Trace.V1;
using ProtoBuf;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using System.Net;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Otel.Proxy.Interfaces;

namespace Otel.Proxy.Tests.Setup;

public class OtelProxyAppFactory : WebApplicationFactory<Program>
{
    public List<ExportTraceServiceRequest> ReceivedExportRequests => _exportTraceServiceRequests.GetValueOrDefault(Activity.Current?.TraceId.ToString() ?? "", new());
    private readonly ConcurrentDictionary<string, List<ExportTraceServiceRequest>> _exportTraceServiceRequests = new();
    private readonly HttpClientInterceptorOptions _httpClientInterceptorOptions = new();
    private readonly TracerProvider? _tracerProvider;
    private readonly List<ISampler> _samplers;
    private static readonly bool _registeredExceptionHandler = false;

    public OtelProxyAppFactory(TracerProvider? tracerProvider, List<ISampler> samplers = null!)
    {
        if (!_registeredExceptionHandler)
            AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
            {
                if (args.Exception.Source == "Shouldly" ||
                    args.Exception.Source == "xunit.assert")
                    Activity.Current?.AddTag("test.outcome", "failed");
            };
        SetupInterceptor();
        _tracerProvider = tracerProvider;
        _samplers = samplers ?? new List<ISampler>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config => {
            config.AddInMemoryCollection(new List<KeyValuePair<string, string?>> {
               new ("Backend:IsMultiTenant", "true") 
            });
        });
        builder.ConfigureServices(services => {
            services.AddLogging(l => l.ClearProviders());
            services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                _ => new HttpClientInterceptionFilter(_httpClientInterceptorOptions));
            if (_tracerProvider != null)
                services.AddSingleton(_tracerProvider);
            if (_samplers.Any())
                services.AddSingleton(new CompositeSampler(_samplers));
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
        client.DefaultRequestHeaders.Add("x-tenant-id", Activity.Current?.TraceId.ToString());

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
                    _exportTraceServiceRequests
                        .GetOrAdd(Activity.Current?.TraceId.ToString() ?? "", new List<ExportTraceServiceRequest>())
                        .Add(exportRequest);
                }
            })
            .RegisterWith(_httpClientInterceptorOptions);
    }
}
