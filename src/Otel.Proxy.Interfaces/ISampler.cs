namespace Otel.Proxy.Interfaces;

/// <summary>
/// Defines classes that can get a sample rate for a given key
/// </summary>
public interface ISampler
{
    public string Name { get; }
    /// <summary>
    /// Get's the stored sample rate for the given key.
    /// </summary>
    /// <param name="key">The SampleKey to lookup</param>
    /// <returns></returns>
    public Task<double> GetSampleRate(string key);
    
    public Task<bool> ShouldSample(List<KeyValuePair<string, object>> tags);

    public Task<string> GenerateKey(List<KeyValuePair<string, object>> tags);
}

/// <summary>
/// Defines samplers that need to be periodically updated
/// </summary>
public interface ISamplerRateUpdater
{
    /// <summary>
    /// Updates all the stored sample rates 
    /// </summary>
    /// <returns></returns>
    public Task UpdateAllSampleRates();
}

public interface ISamplerKeyGenerator
{
}


public record SampleCondition(string Key, object Value, ConditionsOperator Operator);

public enum ConditionsOperator
{
    Unknown,
    Equals
}