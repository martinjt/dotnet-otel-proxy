namespace Otel.Proxy.Tests.Sampling;

public static class DictionaryExtensions
{
    public static void Add(this Dictionary<string, object> dict, List<KeyValuePair<string, object>> attributes)
    {
        attributes.ForEach(a => dict.Add(a.Key, a.Value));
    }
    public static void Add(this Dictionary<string, object> dict, Dictionary<string, object> attributes)
    {
        foreach (var kvp in attributes)
            dict.Add(kvp.Key, kvp.Value);
    }
}
