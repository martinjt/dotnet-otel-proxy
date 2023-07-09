using Otel.Proxy.Setup;
using Otel.Proxy.TraceRepository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(o => o.InputFormatters.Add(new ProtobufInputFormatter()))
    .ConfigureApplicationPartManager(o => 
        o.FeatureProviders.Add(new InternalControllerFeatureProvider()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TraceProcessor>();
builder.Services.AddSingleton<ITraceRepository, InMemoryTraceRepository>();

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


public partial class Program {}
