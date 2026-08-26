using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIContextProvider.Mem0.Mem0Provider.Models;

public sealed class Mem0SearchResponse
{
    [JsonPropertyName("results")]
    public IReadOnlyList<Mem0SearchResult> Results { get; init; } = [];
}