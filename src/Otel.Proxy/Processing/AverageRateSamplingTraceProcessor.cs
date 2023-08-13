using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Otel.Proxy.Processing;

internal class AverageRateSamplingTraceProcessor : ITraceProcessor
{
    private const int KeepAllTraces = 1;
    private readonly Random _random = new();
    private readonly ITraceRepository _traceRepository;
    private readonly GenericSampleKeyGenerator _samplerKeyGenerator;
    private readonly InMemoryAverageRateSampler _averageRateSampler;
    private readonly HoneycombExporter _honeycombExporter;

    public AverageRateSamplingTraceProcessor(
        ITraceRepository traceRepository,
        GenericSampleKeyGenerator samplerKeyGenerator,
        InMemoryAverageRateSampler averageRateSampler,
        HoneycombExporter honeycombExporter)
    {
        _traceRepository = traceRepository;
        _samplerKeyGenerator = samplerKeyGenerator;
        _averageRateSampler = averageRateSampler;
        _honeycombExporter = honeycombExporter;
    }

    public async Task ProcessTrace(byte[] traceId)
    {
        var trace = await _traceRepository.GetTrace(traceId);

        var sampleKey = await GenerateSampleKeyFromTraceAttributes(trace);

        var sampleRateForKey = await _averageRateSampler.GetSampleRate(sampleKey);

        if (sampleRateForKey != KeepAllTraces &&
            _random.NextInt64(1, (int)sampleRateForKey) == 1)
        {
            await SendTrace(trace, sampleRateForKey);
        }

        await _traceRepository.DeleteTrace(traceId);
    }

    private async Task SendTrace(IEnumerable<SpanRecord> trace, double sampleRateForKey)
    {
        var exportRequest = new ExportTraceServiceRequest();
        exportRequest.ResourceSpans.AddRange(
            trace.Select(rs =>
            {
                var resourceSpan = new ResourceSpans()
                {
                    Resource = rs.Resource
                };
                resourceSpan.ScopeSpans.AddRange(
                    rs.Spans.Select(s =>
                    {
                        var scopeSpans = new ScopeSpans();
                        scopeSpans.Spans.AddRange(rs.Spans.Select(s =>
                        {
                            s.Attributes.Add(new KeyValue
                            {
                                Key = "SampleRate",
                                Value = {
                                    IntValue = (int)sampleRateForKey
                                }
                            });
                            return s;
                        }));
                        return scopeSpans;
                    }));
                return resourceSpan;
            })
        );

        await _honeycombExporter.SendRequestToHoneycomb(exportRequest);
    }

    private async Task<string> GenerateSampleKeyFromTraceAttributes(IEnumerable<SpanRecord> spanRecords)
    {
        var attributes = new List<KeyValuePair<string, AnyValue>>();
        attributes.AddRange(
            spanRecords.SelectMany(rs => rs.Resource.Attributes)
                 .Select(kv => new KeyValuePair<string, AnyValue>(kv.Key, kv.Value)));
        attributes.AddRange(spanRecords.SelectMany(rs => rs.Spans)
                                  .SelectMany(s => s.Attributes)
                                  .Select(kv => new KeyValuePair<string, AnyValue>(kv.Key, kv.Value)));

        return await _samplerKeyGenerator.GenerateKey(attributes);
    }
}