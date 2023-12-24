namespace Otel.Proxy.Setup;

public class BackendSettings
{
    public enum BackendType
    {
        InMemory,
        Redis
    }
    public bool IsMultiTenant { get; set; } = false;
    public BackendType Type { get; set; }
    public string TenantHeader { get; set; } = "x-tenant-id";
    public string? RedisConnectionString { get; set; } = "localhost:6379";
}

public class ProcessingSettings
{
    public enum ProcessingType
    {
        NoSampling = 0,
        AverageRate = 1
    }

    public bool DryRunEnabled { get; set; } = true;
    public ProcessingType TraceProcessor { get; set; }

    public int TraceExpirationInSeconds { get; set; } = 0;
    public int RootSpanProcessingDelayInSeconds { get; set; } = 0;
    public int TraceProcessingTimeoutInSeconds { get; set; } = 0;
}
