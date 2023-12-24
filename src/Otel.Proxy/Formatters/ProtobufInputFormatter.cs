using System.Collections.Concurrent;
using System.Reflection;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;

namespace Otel.Proxy.Formatters;

public class ProtobufInputFormatter : InputFormatter
{
    private const string ParserPropertyName = "Parser";
    private const string ParseFromPropertyName = "ParseFrom";
    private readonly int _memoryBufferThreshold = 256 * 1024;
    private readonly ConcurrentDictionary<Type, (MethodInfo, object?)> _parsers = new();

    public ProtobufInputFormatter()
    {
        SupportedMediaTypes.Add("application/x-protobuf");
    }

    protected override bool CanReadType(Type type)
        => type.IsAssignableTo(typeof(IMessage));

    public (MethodInfo, object?) GetParserInfo(Type modelType)
    {
        var parserProperty = modelType.GetProperty(ParserPropertyName);
        var parser = parserProperty!.GetValue(null, null);
        var methodInfo = parser!.GetType().GetMethods()
            .First(mi =>
                mi.Name == ParseFromPropertyName &&
                mi.GetParameters().Any(pi => pi.ParameterType == typeof(Stream)));
        return (methodInfo, parser);
                
    }
    private InputFormatterResult Parse(Type modelType, Stream stream)
    {
        var (parseFrom, parser) = _parsers.GetOrAdd(modelType, GetParserInfo);
        var result = parseFrom.Invoke(parser, new[] { stream });

        return InputFormatterResult.Success(result);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        using var readStream = new FileBufferingReadStream(context.HttpContext.Request.Body, _memoryBufferThreshold);
        await readStream.DrainAsync(CancellationToken.None);
        readStream.Seek(0, SeekOrigin.Begin);
        return Parse(context.ModelType, readStream);
    }
}