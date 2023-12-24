namespace Otel.Proxy.Processing;

internal interface IProcessScheduler
{
    Task ScheduleTraceProcessing(byte[] traceId, int delaySeconds);
}