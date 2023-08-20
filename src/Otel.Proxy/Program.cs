using Microsoft.Extensions.Options;
using Otel.Proxy.Setup;
using Otel.Proxy.TraceRepository;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel => {
    kestrel.ListenAnyIP(4317);
    kestrel.ListenAnyIP(4318);
});

builder.Services.AddControllers(o => o.InputFormatters.Add(new ProtobufInputFormatter()))
    .ConfigureApplicationPartManager(o =>
        o.FeatureProviders.Add(new InternalControllerFeatureProvider()));

builder.Services.AddGrpc();
builder.Services.AddOptions<BackendSettings>().Bind(builder.Configuration.GetSection("Backend"));
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<HoneycombExporter>();
builder.Services.AddSingleton<ITraceProcessor, SamplingTraceProcessor>();
builder.Services.AddSamplers();
builder.Services.AddStorageBackend();

var app = builder.Build();

app.UseTenantIdMiddleware();
app.UseAuthorization();

app.MapWhen(context => context.Connection.LocalPort == 4317,
    grpcPort => grpcPort.UseRouting().UseEndpoints(e => e.MapGrpcService<TraceGrpcService>()));
app.MapWhen(context => context.Connection.LocalPort == 4318,
    httpPort => httpPort.UseRouting().UseEndpoints(e => e.MapControllers()));

app.Run();


public partial class Program { }


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
}
