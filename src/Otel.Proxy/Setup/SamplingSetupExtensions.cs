using Microsoft.Extensions.Options;

namespace Otel.Proxy.Setup;

internal static class SamplingSetupExtensions
{
    public static void AddSamplers(this IServiceCollection services)
    {
        services.AddSingleton(sp => {
            var processorSettings = sp.GetRequiredService<IOptions<ProcessingSettings>>();
            var samplerList = new List<ISampler>();
            if (processorSettings.Value.TraceProcessor == ProcessingSettings.ProcessingType.AverageRate)
                samplerList.Add(new InMemoryAverageRateSampler(20, 
                    new HashSet<string> { "http.method", "http.status_code" }));
            
            return new CompositeSampler(samplerList);
        });
    } 
}