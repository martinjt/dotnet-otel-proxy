namespace Otel.Proxy.Interfaces;

/// <summary>
/// Defines classes that can get a sample rate for a given key
/// </summary>
public interface ISamplerRate
{
    /// <summary>
    /// Get's the stored sample rate for the given key.
    /// </summary>
    /// <param name="key">The SampleKey to lookup</param>
    /// <returns></returns>
    public Task<double> GetSampleRate(string key);
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
    public Task<string> GenerateKey(List<KeyValuePair<string, string>> tags);
}
