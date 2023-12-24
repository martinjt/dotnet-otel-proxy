using System.Collections.Concurrent;

namespace Otel.Proxy.Processing;

internal class InMemoryProcessScheduler : IProcessScheduler
{
    private readonly ITraceProcessor _traceProcessor;
    private readonly ConcurrentDictionary<byte[], Timer> _timers = new();

    public InMemoryProcessScheduler(ITraceProcessor traceProcessor)
    {
        _traceProcessor = traceProcessor;
    }

    public async Task ScheduleTraceProcessing(byte[] traceId, int delaySeconds)
    {
        if (delaySeconds == 0)
            await _traceProcessor.ProcessTrace(traceId);
        else
        {
            var timer = new Timer(async _ => { 
                await _traceProcessor.ProcessTrace(traceId); 
                _timers.TryRemove(traceId, out var _);
            });
            _timers.TryAdd(traceId, timer);
            timer.Change(TimeSpan.FromSeconds(delaySeconds), Timeout.InfiniteTimeSpan);
        }
    }
}