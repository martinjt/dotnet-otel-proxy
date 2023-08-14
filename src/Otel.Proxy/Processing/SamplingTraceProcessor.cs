using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Otel.Proxy.Processing;

internal class SamplingTraceProcessor : ITraceProcessor
{
    private const int KeepAllTraces = 1;
    private readonly Random _random = new();
    private readonly ITraceRepository _traceRepository;
    private readonly CompositeSampler _compositeSampler;
    private readonly HoneycombExporter _honeycombExporter;

    public SamplingTraceProcessor(
        ITraceRepository traceRepository,
        CompositeSampler compositeSampler,
        HoneycombExporter honeycombExporter)
    {
        _traceRepository = traceRepository;
        _compositeSampler = compositeSampler;
        _honeycombExporter = honeycombExporter;
    }

    public async Task ProcessTrace(byte[] traceId)
    {
        var trace = await _traceRepository.GetTrace(traceId);

        if (_compositeSampler.SamplingActive)
        {
            var attributes = GetAllAttributesAsList(trace);

            var sampleRateForKey = await _compositeSampler.GetSampleRate(attributes);

            if (sampleRateForKey != KeepAllTraces &&
                _random.NextInt64(1, (int)sampleRateForKey) == 1)
            {
                await SendTrace(trace, sampleRateForKey);
            }
        }
        else
            await SendTrace(trace, KeepAllTraces);

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
                                Value = new AnyValue {
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

    private static List<KeyValuePair<string, object>> GetAllAttributesAsList(IEnumerable<SpanRecord> spanRecords)
    {
        var attributes = new List<KeyValuePair<string, object>>();
        attributes.AddRange(
            spanRecords.SelectMany(rs => rs.Resource.Attributes)
                 .Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value)));
        attributes.AddRange(spanRecords.SelectMany(rs => rs.Spans)
                                  .SelectMany(s => s.Attributes)
                                  .Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value)));

        return attributes;
    }
}