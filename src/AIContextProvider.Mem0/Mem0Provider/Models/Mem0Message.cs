using System.Text.Json.Serialization;

namespace AIContextProvider.Mem0.Mem0Provider.Models;

public sealed record Mem0Message(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);