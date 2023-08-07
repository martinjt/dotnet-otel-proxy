public interface ISampleKeyGenerator
{
    /// <summary>
    /// Generates a single string key from the given tags.
    /// </summary>
    /// <returns>The key generated from the </returns>
    string GenerateKey(List<KeyValuePair<string, string>> tags);
}
