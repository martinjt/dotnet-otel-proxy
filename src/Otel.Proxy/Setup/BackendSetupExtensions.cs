using Microsoft.Extensions.Options;
using StackExchange.Redis;

internal static class StorageExtensionsSetup
{
    public static void AddStorageBackend(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryTraceStore>();
        services.AddSingleton<TenantInMemoryStoreAccessor>();

        services.AddSingleton<ITraceRepository>(sp =>
        {
            var backendSettings = sp.GetRequiredService<IOptions<BackendSettings>>();
            if (backendSettings.Value.Type == BackendSettings.BackendType.InMemory)
            {
                if (backendSettings.Value.IsMultiTenant)
                    return new InMemoryTraceRepository(() =>
                    {
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
    }
}