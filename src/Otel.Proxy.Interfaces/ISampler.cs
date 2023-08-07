public interface ISampler
{
    /// <summary>
    /// Get's the stored sample rate for the given key.
    /// </summary>
    /// <param name="key">The SampleKey to lookup</param>
    /// <returns></returns>
    public Task<int> GetSampleRate(string key);
    
    /// <summary>
    /// Updates all the stored sample rates 
    /// </summary>
    /// <returns></returns>
    public Task UpdateAllSampleRates();
}