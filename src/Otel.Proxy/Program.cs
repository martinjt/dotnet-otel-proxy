using Microsoft.Extensions.Options;
using Otel.Proxy.Setup;
using Otel.Proxy.TraceRepository;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(o => o.InputFormatters.Add(new ProtobufInputFormatter()))
    .ConfigureApplicationPartManager(o =>
        o.FeatureProviders.Add(new InternalControllerFeatureProvider()));
builder.Services.AddOptions<BackendSettings>().Bind(builder.Configuration.GetSection("Backend"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TraceProcessor>();
builder.Services.AddSingleton<ITraceRepository>(sp =>
{
    var backendSettings = sp.GetRequiredService<IOptions<BackendSettings>>();
    if (backendSettings.Value.Type == BackendSettings.BackendType.InMemory)
    {
        return new InMemoryTraceRepository();
    }
    else
    {
        var redis = ConnectionMultiplexer.Connect(backendSettings.Value.RedisConnectionString);
        return new RedisTraceRepository(redis.GetDatabase());
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();


public partial class Program { }


public class BackendSettings
{
    public enum BackendType
    {
        InMemory,
        Redis
    }
    public BackendType Type { get; set; }
    public string? RedisConnectionString { get; set; } = "localhost:6379";
}

