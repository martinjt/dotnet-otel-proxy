using Otel.Proxy.Formatters;
using Otel.Proxy.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel => {
    kestrel.ListenAnyIP(4317);
    kestrel.ListenAnyIP(4318);
});

builder.Services.AddControllers(o => {
    o.InputFormatters.Add(new ProtobufInputFormatter());
    o.OutputFormatters.Add(new ProtobufOutputFormatter());
    })
    .ConfigureApplicationPartManager(o =>
        o.FeatureProviders.Add(new InternalControllerFeatureProvider()));

builder.Services.AddGrpc();
builder.Services.AddOptions<BackendSettings>().Bind(builder.Configuration.GetSection("Backend"));
builder.Services.AddOptions<ProcessingSettings>().Bind(builder.Configuration.GetSection("Processing"));
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<HoneycombExporter>();
builder.Services.AddSingleton<ITraceProcessor, SamplingTraceProcessor>();
builder.Services.AddSamplers();
builder.AddStorageBackend();

var app = builder.Build();

app.UseTenantIdMiddleware();
app.UseAuthorization();

app.MapWhen(context => context.Connection.LocalPort == 4317,
    grpcPort => grpcPort.UseRouting().UseEndpoints(e => e.MapGrpcService<TraceGrpcService>()));
app.MapWhen(context => context.Connection.LocalPort == 4318,
    httpPort => httpPort.UseRouting().UseEndpoints(e => e.MapControllers()));

app.Run();


public partial class Program { }
