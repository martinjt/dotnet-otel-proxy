namespace Otel.Proxy.Processing;

internal interface ITraceProcessor
{
    Task ProcessTrace(byte[] traceId);
}