using Microsoft.Extensions.Options;
using Otel.Proxy.Setup;
using StackExchange.Redis;

internal static class StorageExtensionsSetup
{
    public static void AddStorageBackend(this IHostApplicationBuilder builder)
    {
        var backendSettings = builder.Configuration.GetSection("Backend").Get<BackendSettings>();
        backendSettings ??= new BackendSettings {
                IsMultiTenant = false,
                Type = BackendSettings.BackendType.InMemory
            };

        builder.Services.AddSingleton<InMemoryTraceStore>();
        builder.Services.AddSingleton<TenantInMemoryStoreAccessor>();
        if (backendSettings.Type == BackendSettings.BackendType.InMemory)
        {
            builder.Services.AddInMemoryBackend(backendSettings);
            return;
        }

        if (backendSettings.Type == BackendSettings.BackendType.Redis)
        {
            builder.Services.AddRedisBackend(backendSettings);
            return;
        }
    }

    private static void AddInMemoryBackend(this IServiceCollection services, BackendSettings backendSettings)
    {
        services.AddSingleton<ITraceRepository>(sp =>
        {
            if (backendSettings.IsMultiTenant)
                return new InMemoryTraceRepository(() =>
                {
                    var httpAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                    if (httpAccessor.HttpContext == null)
                        throw new InvalidOperationException("HttpContext is null");

                    var tenantAccessor = sp.GetRequiredService<TenantInMemoryStoreAccessor>();
                    return tenantAccessor.GetTenantStore(
                        httpAccessor.HttpContext
                            .Request.Headers[backendSettings.TenantHeader].First() ?? "");
                });

            return new InMemoryTraceRepository(() => sp.GetRequiredService<InMemoryTraceStore>());
        });

        services.AddSingleton<IProcessScheduler>(sp =>
        {
            return new InMemoryProcessScheduler(sp.GetRequiredService<ITraceProcessor>());
        });
    }

    private static void AddRedisBackend(this IServiceCollection services, BackendSettings backendSettings)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp => {
            if (string.IsNullOrEmpty(backendSettings.RedisConnectionString))
                throw new ArgumentException("You must provide a Redis connection string when using Redis backend");

            var connection = ConnectionMultiplexer.Connect(backendSettings.RedisConnectionString);
            return connection;
        });

        services.AddSingleton<ITraceRepository>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var processingSettings = sp.GetRequiredService<IOptions<ProcessingSettings>>();
            return new RedisTraceRepository(redis.GetDatabase(), processingSettings);
        });

        services.AddSingleton<IProcessScheduler>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var logger = sp.GetRequiredService<ILogger<RedisProcessScheduler>>();
            return new RedisProcessScheduler(redis.GetDatabase(), redis, sp.GetRequiredService<ITraceProcessor>(), logger);
        });
    }
}