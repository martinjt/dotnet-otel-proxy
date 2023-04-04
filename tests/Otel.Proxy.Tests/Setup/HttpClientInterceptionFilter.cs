using JustEat.HttpClientInterception;
using Microsoft.Extensions.Http;

namespace Otel.Proxy.Tests.Setup;

public class HttpClientInterceptionFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly HttpClientInterceptorOptions _options;

    public HttpClientInterceptionFilter(HttpClientInterceptorOptions options) => _options = options;

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
        (builder) =>
        {
            next(builder);

            builder.AdditionalHandlers.Add(_options.CreateHttpMessageHandler());
        };
}