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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TraceProcessor>();
builder.Services.AddSingleton<InMemoryTraceStore>();
builder.Services.AddSingleton<TenantInMemoryStoreAccessor>();
builder.Services.AddSingleton<ITraceRepository>(sp =>
{
    var backendSettings = sp.GetRequiredService<IOptions<BackendSettings>>();
    if (backendSettings.Value.Type == BackendSettings.BackendType.InMemory)
    {
        if (backendSettings.Value.IsMultiTenant)
            return new InMemoryTraceRepository(() => {
                var httpAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                if (httpAccessor.HttpContext == null)
                    throw new InvalidOperationException("HttpContext is null");

                var tenantAccessor = sp.GetRequiredService<TenantInMemoryStoreAccessor>();
                return tenantAccessor.GetTenantStore(
                    httpAccessor.HttpContext
                        .Request.Headers[backendSettings.Value.TenantHeader].First() ?? "");
            });

        return new InMemoryTraceRepository(() => sp.GetRequiredService<InMemoryTraceStore>());
    }
    else
    {
        if (string.IsNullOrEmpty(backendSettings.Value.RedisConnectionString))
            throw new ArgumentException("You must provide a Redis connection string when using Redis backend");

        var redis = ConnectionMultiplexer.Connect(backendSettings.Value.RedisConnectionString);
        return new RedisTraceRepository(redis.GetDatabase());
    }
});

var app = builder.Build();

app.UseTenantIdMiddleware();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

