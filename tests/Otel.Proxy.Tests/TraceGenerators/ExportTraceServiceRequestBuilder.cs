using System.Diagnostics;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using System.Text;

namespace Otel.Proxy.Tests.TraceGenerators;

public static class ExportTraceServiceRequestBuilder
{
    public static ExportTraceServiceRequest Build(List<TraceModel> traces)
    {
        var exportRequest = new ExportTraceServiceRequest();

        var resources = new Dictionary<Guid, ServiceModel>();
        var allSpans = new List<SpanModel>();

        foreach (var trace in traces)
        {
            if (trace.RootSpan != null)
            {
                trace.RootSpan.TraceId = trace.TraceId;
                allSpans.Add(trace.RootSpan);
            }

            if (trace.ChildSpans.Any())
                allSpans.AddRange(trace.ChildSpans.Flatten(
                    trace.TraceId,
                    trace.RootSpan?.SpanId ?? ActivitySpanId.CreateRandom(),
                    cs => cs.ChildSpans));
        }
        var tracesGroupedByService = allSpans
            .GroupBy(s => s.Service);

        foreach (var service in tracesGroupedByService)
        {
            var resource = new Resource
            {
                Attributes = {
                    new KeyValue
                    {
                        Key = "service.name",
                        Value = new AnyValue { StringValue = service.Key.Name }
                    },
                    service.Key.Attributes.Select(a => new KeyValue
                    {
                        Key = a.Key,
                        Value = new AnyValue { StringValue = a.Value }
                    })
                }
            };

            var resourceSpans = new ResourceSpans
            {
                Resource = resource
            };

            resourceSpans.ScopeSpans.AddRange(service.Select(s =>
            {
                var scoped = new ScopeSpans();
                var span = new Span
                {
                    TraceId = Encoding.UTF8.GetBytes(s.TraceId.ToString()),
                    SpanId = Encoding.UTF8.GetBytes(s.SpanId.ToString()),
                    ParentSpanId = s.ParentSpanId.HasValue ? Encoding.UTF8.GetBytes(s.ParentSpanId.Value.ToString()) : default,
                    Attributes = {
                        s.Attributes.Select(a => new KeyValue
                        {
                            Key = a.Key,
                            Value = new AnyValue { StringValue = a.Value }
                        })
                    }
                };
                scoped.Spans.Add(span);
                return scoped;
            }));
            exportRequest.ResourceSpans.Add(resourceSpans);
        }
        return exportRequest;

    }
    private static List<SpanModel> Flatten(this IEnumerable<SpanModel> spans,
        ActivityTraceId traceId,
        ActivitySpanId? parentSpanId = null,
        Func<SpanModel, IEnumerable<SpanModel>> childrenSelector = null!)
    {
        var result = new List<SpanModel>();
        foreach (var span in spans)
        {
            span.TraceId = traceId;
            if (parentSpanId.HasValue && span.ParentSpanId == null)
                span.ParentSpanId = parentSpanId.Value;

            result.Add(span);

            var children = childrenSelector(span);
            if (children != null)
            {
                result.AddRange(children.Flatten(traceId, span.SpanId, childrenSelector));
            }
        }
        return result;
    }

    private static TCollection Add<TCollection, TItem>(
        this TCollection destination,
        IEnumerable<TItem> source)
        where TCollection : ICollection<TItem>
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);

        if (destination is List<TItem> list)
        {
            list.AddRange(source);
            return destination;
        }

        foreach (var item in source)
        {
            destination.Add(item);
        }

        return destination;
    }
}
