using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tpb => 
        tpb
        .ConfigureResource(rs => rs.AddService("sample-proxy-generator"))
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(o => {
            o.Endpoint = new Uri("http://localhost:50656/v1/traces");
            o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        })
    );
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
