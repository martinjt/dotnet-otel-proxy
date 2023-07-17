using System.Collections.Concurrent;

public class TenantInMemoryStoreAccessor
{
    private ConcurrentDictionary<string, InMemoryTraceStore> _tenantStoreDictionary = new();
    internal InMemoryTraceStore GetTenantStore(string tenantId)
    {
        return _tenantStoreDictionary.GetOrAdd(tenantId, (t) => new InMemoryTraceStore());
    }

    public void RemoveTenantStore(string tenantId)
    {
        _tenantStoreDictionary.TryRemove(tenantId, out var _);
    }
}