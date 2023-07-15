using System.Diagnostics;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using System.Text;

internal static partial class TraceGenerator
{
    public static ExportTraceServiceRequest CreateValidTraceExport(string serviceName = "test-service")
    {
        var exportRequest = new ExportTraceServiceRequest();
        var resource = new Resource();
        resource.Attributes.Add(new KeyValue
        {
            Key = "service.name",
            Value = new AnyValue { StringValue = serviceName }
        });
        var spans = new ScopeSpans();
        spans.Spans.Add(new Span
        {
            TraceId = Encoding.UTF8.GetBytes(ActivityTraceId.CreateRandom().ToString()),
            SpanId = Encoding.UTF8.GetBytes(ActivitySpanId.CreateRandom().ToString()),
            Attributes = {
                { new KeyValue {
                    Key = "my_attribute",
                    Value = new AnyValue { StringValue = "my value"}
                }}
            }
        });
        var resourceSpans = new ResourceSpans
        {
            Resource = resource
        };
        resourceSpans.ScopeSpans.Add(spans);
        exportRequest.ResourceSpans.Add(resourceSpans);

        return exportRequest;
    }
}

public class ExportServiceRequestBuilder
{
    private readonly List<Resource> _resources = new(); 
    private readonly List<TraceBuilder> _traceBuilders = new();

    public ExportServiceRequestBuilder WithService()
    {
        return WithService("test-service");
    }


    public ExportServiceRequestBuilder WithService(
        string serviceName,
        (string key, string value) attribute = default)
    {
        return WithService(serviceName, new[] { attribute });
    }

    public ExportServiceRequestBuilder WithService(
        string serviceName,
        IEnumerable<(string key, string value)> attributes)
    {
        var resource = new Resource();
        resource.Attributes.Add(new KeyValue
        {
            Key = "service.name",
            Value = new AnyValue { StringValue = serviceName }
        });
        if (attributes != null)
            resource.Attributes.AddRange(attributes.Select(a => new KeyValue {
                Key = a.key,
                Value = new AnyValue {
                    StringValue = a.value
                }
            }));
        _resources.Add(resource);
        return this;
    }

    public ExportServiceRequestBuilder WithTrace(Action<TraceBuilder> traceOptions)
    {
        var traceBuilder = new TraceBuilder();
        _traceBuilders.Add(traceBuilder);
        traceOptions.Invoke(traceBuilder);
        return this;
    }

    public ExportServiceRequestBuilder WithTrace(ActivityTraceId traceId, Action<TraceBuilder> traceOptions)
    {
        var traceBuilder = new TraceBuilder(traceId);
        _traceBuilders.Add(traceBuilder);
        traceOptions.Invoke(traceBuilder);
        return this;
    }

    public ExportTraceServiceRequest Build()
    {
        var exportRequest = new ExportTraceServiceRequest();

        foreach (var resource in _resources)
        {
            var serviceName = resource
                .Attributes
                .First(s => s.Key == "service.name").Value.StringValue;
            var resourceSpans = new ResourceSpans {
                Resource = resource
            };
            resourceSpans.ScopeSpans.AddRange(
                _traceBuilders.SelectMany(tb => 
                {
                    var scopeSpans = new List<ScopeSpans>();
                    scopeSpans.AddRange(tb.Spans
                        .SelectMany(s => s.GetSpansForService(serviceName))
                        .Select(s => {
                            var scoped = new ScopeSpans();
                            var span = s.ConvertToSpan();
                            span.Attributes.AddRange(s.Attributes.Select(a => new KeyValue {
                                Key = a.Key,
                                Value = new AnyValue { StringValue = a.Value.ToString() }
                            }));
                            scoped.Spans.Add(span);
                            return scoped;
                        }));
                    return scopeSpans;
                })
            );

            exportRequest.ResourceSpans.Add(resourceSpans);

        }

        return exportRequest;
    }
}

public class TraceBuilder
{
    private readonly ActivityTraceId? _traceId;
    public List<SpanBuilder> Spans { get; } = new();

    public TraceBuilder(ActivityTraceId? traceId = null)
    {
        _traceId = traceId ?? ActivityTraceId.CreateRandom();
    }

    public SpanBuilder WithRootSpan(Action<SpanBuilder> spanBuilder = null!)
    {
        var spanBuilderObject = new SpanBuilder(_traceId!.Value);
        Spans.Add(spanBuilderObject);
        spanBuilder?.Invoke(spanBuilderObject);
        return spanBuilderObject;
    }

    public SpanBuilder WithSpan(Action<SpanBuilder> spanBuilder = null!, 
        ActivitySpanId? spanId = null!,
        ActivitySpanId? parentSpanId = null!)
    {
        var spanBuilderObject = new SpanBuilder(_traceId!.Value, spanId, parentSpanId);
        Spans.Add(spanBuilderObject);
        spanBuilder?.Invoke(spanBuilderObject);
        return spanBuilderObject;
    }

    public SpanBuilder CreateSpanWithRandomParent()
    {
        var spanBuilder = new SpanBuilder(
            _traceId!.Value, parentSpanId: ActivitySpanId.CreateRandom());
        Spans.Add(spanBuilder);
        return spanBuilder;
    }
}

public class SpanBuilder
{
    private readonly ActivityTraceId _traceId;
    private readonly ActivitySpanId _spanId;
    private readonly ActivitySpanId? _parentSpanId;
    public List<KeyValuePair<string, object>> Attributes { get; } = new();

    private string _serviceName = "test-service";
    public List<SpanBuilder> ChildSpans = new();

    public SpanBuilder(
        ActivityTraceId traceId,
        ActivitySpanId? spanId = null,
        ActivitySpanId? parentSpanId = null)
    {
        _traceId = traceId;
        _spanId = spanId ?? ActivitySpanId.CreateRandom();
        _parentSpanId = parentSpanId;
    }

    public SpanBuilder WithAttribute(string key, string value)
    {
        Attributes.Add(new(key, value));
        return this;
    }
    public SpanBuilder WithAttributes(IEnumerable<KeyValuePair<string, object>> attributes)
    {
        Attributes.AddRange(attributes);
        return this;
    }

    public SpanBuilder ForService(string serviceName)
    {
        _serviceName = serviceName;
        return this;
    }

    public SpanBuilder WithChildSpan(Action<SpanBuilder> spanBuilder)
    {
        var spanBuilderObject = new SpanBuilder(_traceId!);
        ChildSpans.Add(spanBuilderObject);
        spanBuilder.Invoke(spanBuilderObject);
        return spanBuilderObject;
    }

    public List<SpanBuilder> GetSpansForService(string serviceName)
    {
        var list = new List<SpanBuilder>();
        if (_serviceName == serviceName)
            list.Add(this);

        list.AddRange(ChildSpans.SelectMany(s => s.GetSpansForService(serviceName)));
        return list;
    }

    public Span ConvertToSpan()
    {
        var span = new Span();
        span.SpanId = System.Text.Encoding.UTF8.GetBytes(_spanId.ToString());
        span.ParentSpanId = _parentSpanId.HasValue ? System.Text.Encoding.UTF8.GetBytes(_parentSpanId.Value.ToString()) : default;
        span.TraceId = System.Text.Encoding.UTF8.GetBytes(_traceId.ToString());

        return span;
    }
}