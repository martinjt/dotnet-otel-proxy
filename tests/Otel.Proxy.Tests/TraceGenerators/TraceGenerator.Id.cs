using System.Diagnostics;

internal static partial class TraceGenerator
{
    private static byte[] GenerateRandomTraceId()
    {
        var traceId = new Span<Byte>();
        ActivityTraceId.CreateRandom().CopyTo(traceId);
        return traceId.ToArray();
    }

    private static byte[] GenerateRandomSpanId()
    {
        var spanId = new Span<Byte>();
        ActivitySpanId.CreateRandom().CopyTo(spanId);
        return spanId.ToArray();
    }
}