using System.Diagnostics;

namespace Otel.Proxy.Tests.TraceGenerators;

public class TraceModel
{
    public ActivityTraceId TraceId { get; set; } = ActivityTraceId.CreateRandom();
    public SpanModel RootSpan { get; set; } = null!;
    public List<SpanModel> ChildSpans { get; set; } = new();
}

public class SpanModel
{
    public ActivityTraceId TraceId { get; set; }
    public ServiceModel Service { get; set; } = new ServiceModel();
    public ActivitySpanId SpanId { get; set; } = ActivitySpanId.CreateRandom();
    public ActivitySpanId? ParentSpanId { get; set; } = null;
    public Dictionary<string,object> Attributes { get; set; } = new();
    public List<SpanModel> ChildSpans { get; set; } = new();
}

public class ServiceModel
{
    public Guid CorrelationForTestBuilder { get; } = Guid.NewGuid(); 
    public string Name { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string,object> Attributes { get; set; } = new();
}
