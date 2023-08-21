using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace Otel.Proxy.Tests.Setup;

/// <summary>
/// Writes links to the Honeycomb UI for all root spans to the console
/// </summary>
public class HoneycombTestOutputHelper
{
    private const string _apiHost = "https://api.honeycomb.io";
    private readonly string _apiKey;
    private readonly string _serviceName;
    private string? _teamSlug;
    private string? _environmentSlug;

    private bool _isEnabled = false;

    public HoneycombTestOutputHelper(IConfiguration config, string serviceName)
    {
        _apiKey = config["HONEYCOMB_API_KEY"] ?? "";
        _serviceName = serviceName;
        try
        {
            InitTraceLinkParameters();
        }
        catch (Exception)
        {
            Console.WriteLine("WARN: Failed to get data from Honeycomb Auth Endpoint");
        }
    }

    private void InitTraceLinkParameters()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return;
        }

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_apiHost)
        };
        httpClient.DefaultRequestHeaders.Add("X-Honeycomb-Team", _apiKey);

        var response = httpClient.GetAsync("/1/auth").GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("WARN: Didn't get a valid response from Honeycomb");
            return;
        }

        var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseString);
        _environmentSlug = authResponse?.Environment?.Slug ?? "";
        _teamSlug = authResponse?.Team?.Slug ?? "";
        if (string.IsNullOrEmpty(_environmentSlug) ||
            string.IsNullOrEmpty(_teamSlug))
        {
            Console.WriteLine("WARN: Team or Environment wasn't returned");
            return;
        }
        _isEnabled = true;
    }

    /// <inheritdoc />
    public void RecordTest(ITestOutputHelper testOutputHelper, Activity activity)
    {
        if (!_isEnabled)
            return;
        testOutputHelper.WriteLine($"Honeycomb link: {GetTraceLink(activity.TraceId.ToString())}");
    }

    private string GetTraceLink(string traceId) =>
        _apiKey.Length == 32
            ? $"http://ui.honeycomb.io/{_teamSlug}/datasets/{_serviceName}/trace?trace_id={traceId}"
            : $"http://ui.honeycomb.io/{_teamSlug}/environments/{_environmentSlug}/datasets/{_serviceName}/trace?trace_id={traceId}";
}

internal class AuthResponse
{
    [JsonPropertyName("environment")]
    public HoneycombEnvironment? Environment { get; set; }

    [JsonPropertyName("team")]
    public Team? Team { get; set; }
}
internal class HoneycombEnvironment
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }
}

internal class Team
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }
}

