using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Otel.Proxy.Setup;


namespace Otel.Proxy.TraceRepository;

internal class RedisTraceRepository : ITraceRepository
{
    private readonly IDatabase _database;
    private readonly IOptions<ProcessingSettings> _processingSettings;

    public RedisTraceRepository(IDatabase database, IOptions<ProcessingSettings> processingSettings)
    {
        _database = database;
        _processingSettings = processingSettings;
    }

    public async Task AddSpans(ExportTraceServiceRequest request)
    {
        foreach (var resourceSpan in request.ResourceSpans)
            foreach (var grouping in resourceSpan
                .ScopeSpans.SelectMany(ss => ss.Spans)
                .GroupBy(s => s.TraceId))
            {
                var traceId = Convert.ToHexString(grouping.Key.ToByteArray());
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
                var currentExpiry = await _database.KeyTimeToLiveAsync(traceId);
                await _database.ListRightPushAsync($"{traceId}:store", ms.ToArray());
                await _database.KeyExpireAsync($"{traceId}:store", TimeSpan.FromMinutes(_processingSettings.Value.TraceProcessingTimeoutInSeconds));
            }
    }

    public async Task DeleteTrace(byte[] traceIdBytes)
    {
        var traceId = Convert.ToHexString(traceIdBytes);
        await _database.KeyDeleteAsync(traceId);
    }

    public async Task<IEnumerable<SpanRecord>> GetTrace(byte[] traceIdBytes)
    {
        var traceId = Convert.ToHexString(traceIdBytes);

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