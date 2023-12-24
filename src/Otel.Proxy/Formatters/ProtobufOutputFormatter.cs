using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Otel.Proxy.Formatters;

public class ProtobufOutputFormatter : OutputFormatter
{

    public ProtobufOutputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/x-protobuf"));
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var response = context.HttpContext.Response;
        MemoryStream stream = new MemoryStream();
        var protobufMessage = context.Object as IMessage;
        protobufMessage.WriteTo(stream);
        var data = stream.ToArray();
        await response.Body.WriteAsync(data, 0, data.Length);
    }
}