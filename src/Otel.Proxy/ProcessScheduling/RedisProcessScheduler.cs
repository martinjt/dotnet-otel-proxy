
using StackExchange.Redis;

namespace Otel.Proxy.Processing;

internal class RedisProcessScheduler : IProcessScheduler
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    private readonly ITraceProcessor _traceProcessor;
    private readonly ILogger<RedisProcessScheduler> _logger;
    private readonly ISubscriber _subscriber;


    public RedisProcessScheduler(IDatabase database, IConnectionMultiplexer connectionMultiplexer, 
        ITraceProcessor traceProcessor, ILogger<RedisProcessScheduler> logger)
    {
        _database = database;
        _connectionMultiplexer = connectionMultiplexer;
        _traceProcessor = traceProcessor;
        _logger = logger;
        _subscriber = _connectionMultiplexer.GetSubscriber();  
        _subscriber.SubscribeAsync(RedisChannel.Pattern("__keyevent@*__:expired"), async (channel, key) =>  
            {
                _logger.LogInformation($"Processing trace {key}");
                if (key.ToString().EndsWith(":processing"))
                {
                    var traceId = key.ToString().Replace(":processing", "");
                    await _traceProcessor.ProcessTrace(Convert.FromHexString(traceId));
                }
            }  
        );
    }

    public async Task ScheduleTraceProcessing(byte[] traceId, int delaySeconds)
    {
        if (delaySeconds == 0)
        {
            await _traceProcessor.ProcessTrace(traceId);
            return;
        }
        var traceIdString = Convert.ToHexString(traceId);
        await _database.SetAddAsync($"{traceIdString}:processing", traceIdString);
        await _database.KeyExpireAsync($"{traceIdString}:processing", TimeSpan.FromSeconds(delaySeconds));
    }
}