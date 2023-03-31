# .NET OpenTelemetry Proxy

This is a proxy for OpenTelemetry Protocol data written in .NET. This is a demo project for my twitch stream at https://twitch.tv/MartinDotNet

The overall goal is to understand how hard Tail-Based sampling is, and try out some new tools too. Some of the things on my list.

* [DotnetIsolator](https://github.com/SteveSandersonMS/DotNetIsolator)
* [Redis Pub/Sub](https://redis.io/docs/manual/pubsub/)
* [Redis Streams](https://redis.io/docs/data-types/streams/)

## Generating OpenTelemetry Protobuf

Run the following command in the root

```shell
protogen --proto_path=opentelemetry-proto/ --csharp_out=src/Otel.Proxy/ **/*.proto
```
