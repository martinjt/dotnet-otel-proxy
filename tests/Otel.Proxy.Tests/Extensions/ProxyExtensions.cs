using System.Net.Http.Headers;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Otel.Proxy.Tests.TraceGenerators;
using ProtoBuf;
using Shouldly;

namespace Otel.Proxy.Tests.Extensions;

internal static class ProxyExtensions
{
    public static async Task<HttpResponseMessage> PostExportRequest(this HttpClient httpClient, ExportTraceServiceRequest? exportRequest, string? tenantId = null!)
    {
        using var ms = new MemoryStream();
        using var content = new StreamContent(ms);
        Serializer.Serialize(ms, exportRequest);
        ms.Seek(0, SeekOrigin.Begin);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        if (tenantId != null)
            content.Headers.Add("x-tenant-id", tenantId);
        var result = await httpClient.PostAsync("/v1/traces", content);
        result.IsSuccessStatusCode.ShouldBeTrue("Error Posting Export Request" + Environment.NewLine + await result.Content.ReadAsStringAsync());
        return result;
    }

    public static async Task<HttpResponseMessage> PostTracesAsExportRequest(this HttpClient httpClient, TraceModel trace, string? tenantId = null!)
        => await httpClient.PostTracesAsExportRequest(new List<TraceModel> { trace }, tenantId);

    public static async Task<HttpResponseMessage> PostTracesAsExportRequest(this HttpClient httpClient, List<TraceModel> traces, string? tenantId = null!)
    {
        var exportRequest = ExportTraceServiceRequestBuilder.Build(traces);

        return await httpClient.PostExportRequest(exportRequest, tenantId);
    }

}
