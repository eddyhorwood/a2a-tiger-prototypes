using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentExecution.TestUtilities;

public class WiremockApi(string host) : IDisposable
{
    private readonly HttpClient _wireMockClient = new() { BaseAddress = new Uri($"{host}/__admin/") };

    public async Task<List<RequestWrapper>> GetRequests()
    {
        var response = await _wireMockClient.GetAsync("requests");
        var content = await response.Content.ReadAsStringAsync();
        var requests = JsonSerializer.Deserialize<RequestsResponse>(content);

        return requests?.Requests ?? [];
    }

    public async Task Reset()
    {
        await _wireMockClient.PostAsync("reset", null);
    }

    public class RequestsResponse
    {
        [JsonPropertyName("requests")] public List<RequestWrapper> Requests { get; set; } = [];
    }

    public class RequestWrapper
    {
        [JsonPropertyName("request")] public Request? Request { get; set; }
        [JsonPropertyName("response")] public Response? Response { get; set; }

        [JsonPropertyName("stubMapping")] public StubMapping? StubMapping { get; set; }
        // Also has Response, response definition, timings etc.
    }

    public class Request
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("method")] public string? Method { get; set; }
        [JsonPropertyName("headers")] public Dictionary<string, string> Headers { get; set; } = [];
        [JsonPropertyName("body")] public string? Body { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("status")]
        public HttpStatusCode? Status { get; set; }
    }

    public class StubMapping
    {
        [JsonPropertyName("scenarioName")] public string? ScenarioName { get; set; }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _wireMockClient.Dispose();
    }
}
