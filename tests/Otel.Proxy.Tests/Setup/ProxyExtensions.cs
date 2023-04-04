using System.Net.Http.Headers;
using OpenTelemetry.Proto.Collector.Trace.V1;
using ProtoBuf;

namespace Otel.Proxy.Tests.Setup;

internal static class ProxyExtensions
{
    public static async Task<HttpResponseMessage> PostExportRequest(this HttpClient httpClient, ExportTraceServiceRequest? exportRequest)
    {
        using var ms = new MemoryStream();
        using var content = new StreamContent(ms);
        Serializer.Serialize(ms, exportRequest);
        ms.Seek(0, SeekOrigin.Begin);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        return await httpClient.PostAsync("/v1/traces", content);
    }

}