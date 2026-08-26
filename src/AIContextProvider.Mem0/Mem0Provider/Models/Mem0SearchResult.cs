using System.Text.Json.Serialization;

namespace AIContextProvider.Mem0.Mem0Provider.Models;

public sealed class Mem0SearchResult
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("memory")]
    public string Memory { get; init; } = string.Empty;

    [JsonPropertyName("score")]
    public double Score { get; init; }
}