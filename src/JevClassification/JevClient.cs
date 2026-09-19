using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JevClassification;

internal sealed class JevClient(string apiKey)
{
    private readonly HttpClient _httpClient = new();

    private static readonly Uri Endpoint = new("https://api.typesafe.ai/v1/systemone");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<JevResponse> EvaluateAsync(object state, Dictionary<string, object> questions)
    {
        string model = "jev-latest";
        JevRequest payload = new(model, state, questions);
        string requestJson = JsonSerializer.Serialize(payload, JsonOptions);

        using HttpRequestMessage request = new(HttpMethod.Post, Endpoint);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        string responseJson = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JevResponse>(responseJson, JsonOptions)!;
    }
}

internal sealed record JevRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("state")] object State,
    [property: JsonPropertyName("questions")] Dictionary<string, object> Questions);

internal sealed record JevResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("answers")] Dictionary<string, JsonElement> Answers,
    [property: JsonPropertyName("usage")] JevUsage Usage);

internal sealed record JevUsage(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens);
