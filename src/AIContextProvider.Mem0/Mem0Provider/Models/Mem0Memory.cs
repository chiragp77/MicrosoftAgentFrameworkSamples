using System.Text.Json.Serialization;

namespace AIContextProvider.Mem0.Mem0Provider.Models;

public sealed class Mem0Memory
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }

    [JsonPropertyName("app_id")]
    public string? AppId { get; init; }
}