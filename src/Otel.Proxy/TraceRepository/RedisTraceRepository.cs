using Google.Protobuf;
using NRedisStack.RedisStackCommands;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using StackExchange.Redis;


namespace Otel.Proxy.TraceRepository;

internal class RedisTraceRepository : ITraceRepository
{
    private readonly IDatabase _database;

    public RedisTraceRepository(IDatabase database)
    {
        _database = database;
    }

    public async Task AddSpans(ExportTraceServiceRequest request)
    {
        foreach (var resourceSpan in request.ResourceSpans)
            foreach (var grouping in resourceSpan
                .ScopeSpans.SelectMany(ss => ss.Spans)
                .GroupBy(s => s.TraceId))
            {
                var traceId = System.Text.Encoding.UTF8.GetString(grouping.Key.Memory.ToArray());
                if (string.IsNullOrEmpty(traceId))
                    throw new Exception("traceId is null or empty");

                var record = new ResourceSpans
                {
                    Resource = resourceSpan.Resource,
                    ScopeSpans = {
                        new ScopeSpans
                        {
                            Spans = { grouping }
                        }
                    }
                };

                using var ms = new MemoryStream();
                record.WriteTo(ms);
                await _database.ListRightPushAsync(traceId, ms.ToArray());
                await _database.KeyExpireAsync(traceId, TimeSpan.FromSeconds(600));
            }
    }

    public async Task<IEnumerable<SpanRecord>> GetTrace(byte[] traceIdBytes)
    {
        var traceId = System.Text.Encoding.UTF8.GetString(traceIdBytes);

        var listResults = await _database.ListRangeAsync(traceId);

        var records = listResults.Select(result =>
        {
            var resourceSpans = ResourceSpans.Parser.ParseFrom(result);
            return new SpanRecord
            {
                Resource = resourceSpans.Resource,
                Spans = resourceSpans.ScopeSpans.SelectMany(ss => ss.Spans).ToList()
            };
        });
        return records;
    }
}