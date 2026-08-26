using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AIContextProvider.Mem0.Mem0Provider.Models;

namespace AIContextProvider.Mem0.Mem0Provider;

public sealed class Mem0Client : IDisposable
{
    private readonly HttpClient _httpClient;

    public Mem0Client(string apiKey)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.mem0.ai/")
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);
    }

    public async Task AddMemories(
        IEnumerable<Mem0Message> messages,
        string userId,
        string? appId = null,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            messages,
            user_id = userId,
            app_id = appId,
            custom_instructions = "Store a memory only when the statement is explicitly about the user and would " +
                "help personalize a future conversation with that user. Valid memories include the user's personal " +
                "details, preferences, constraints, ongoing goals, relationships, and stable circumstances. The user " +
                "must be the subject of the memory, even if their message does not literally use the word 'I'. Never " +
                "store general knowledge, trivia, claims about unrelated people or things, task content, commands, " +
                "questions, temporary requests, conversation summaries, assistant behavior, or assistant uncertainty. " +
                "If the message contains no durable user-profile information, extract no memory.",
            infer = true
        };

        await SendAsync<Mem0AddResponse>(
            HttpMethod.Post,
            "v3/memories/add/",
            request,
            cancellationToken); 
        //Note. New memories are not immediately available on return as they need to be processed and indexed.
        //There are event polling to wait for success but kept out here for simplicity
    }

    public Task<Mem0SearchResponse> SearchAsync(
        string query,
        string userId,
        string? appId = null,
        int numberOfResults = 10,
        CancellationToken cancellationToken = default)
    {
        List<object> filters = [new { user_id = userId }];

        if (!string.IsNullOrWhiteSpace(appId))
        {
            filters.Add(new { app_id = appId });
        }

        var request = new
        {
            query,
            top_k = numberOfResults,
            filters = new Dictionary<string, object>
            {
                ["AND"] = filters
            }
        };

        return SendAsync<Mem0SearchResponse>(HttpMethod.Post, "v3/memories/search/", request, cancellationToken);
    }

    public Task<Mem0Memory> GetAsync(string memoryId, CancellationToken cancellationToken = default) =>
        SendAsync<Mem0Memory>(
            HttpMethod.Get,
            $"v1/memories/{Uri.EscapeDataString(memoryId)}/",
            null,
            cancellationToken);

    public Task<Mem0Memory> UpdateAsync(
        string memoryId,
        string text,
        CancellationToken cancellationToken = default) =>
        SendAsync<Mem0Memory>(
            HttpMethod.Put,
            $"v1/memories/{Uri.EscapeDataString(memoryId)}/",
            new { text },
            cancellationToken);

    public Task DeleteAsync(string memoryId, CancellationToken cancellationToken = default) =>
        SendWithoutResponseAsync(
            HttpMethod.Delete,
            $"v1/memories/{Uri.EscapeDataString(memoryId)}/",
            cancellationToken);

    public Task DeleteAllAsync(
        string userId,
        string appId,
        CancellationToken cancellationToken = default) =>
        SendWithoutResponseAsync(
            HttpMethod.Delete,
            $"v1/memories/?user_id={Uri.EscapeDataString(userId)}&app_id={Uri.EscapeDataString(appId)}",
            cancellationToken);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Mem0 returned {(int)response.StatusCode} ({response.ReasonPhrase}) for {method} {path}: {error}",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new InvalidOperationException("Mem0 returned an empty response.");
    }

    private async Task SendWithoutResponseAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, path);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _httpClient.Dispose();
}

public sealed record Mem0Message(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

public sealed class Mem0AddResponse
{
    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = string.Empty;
}
