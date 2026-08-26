using System.ComponentModel;
using AIContextProvider.Mem0.Mem0Provider.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AIContextProvider.Mem0.Mem0Provider;

public sealed class Mem0AIContextProvider : Microsoft.Agents.AI.AIContextProvider
{
    private readonly Mem0Client _mem0Client;
    private readonly string _userId;
    private readonly string _appId;
    private readonly IReadOnlyList<AITool> _tools;
    private bool _memoryWasMutated;

    public Mem0AIContextProvider(Mem0Client mem0Client, string userId, string appId)
    {
        _mem0Client = mem0Client;
        _userId = userId;
        _appId = appId;
        _tools =
        [
            AIFunctionFactory.Create(UpdateUserMemoryAsync, name: "update_user_memory"),
            AIFunctionFactory.Create(DeleteUserMemoryAsync, name: "delete_user_memory"),
            AIFunctionFactory.Create(DeleteAllUserMemoriesAsync, name: "delete_all_user_memories")
        ];
    }

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        string? query = context.AIContext.Messages?.LastOrDefault(message => message.Role == ChatRole.User)?.Text;

        if (string.IsNullOrWhiteSpace(query))
        {
            return new AIContext { Tools = _tools };
        }

        Mem0SearchResponse response = await _mem0Client.SearchAsync(
            query,
            _userId,
            _appId,
            numberOfResults: 5,
            cancellationToken);

        Mem0SearchResult[] memories = response.Results.Where(result => !string.IsNullOrWhiteSpace(result.Memory)).ToArray();

        return new AIContext
        {
            Instructions = memories.Length == 0
                ? null
                : $"Relevant memories about the user:\n- {string.Join("\n- ", memories.Select(memory => $"[{memory.Id}] {memory.Memory}"))}",
            Tools = _tools
        };
    }

    protected override async ValueTask StoreAIContextAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        if (MemoryWasMutatedByTool())
        {
            return;
        }

        Mem0Message[] messages = context.RequestMessages
            .Where(message => message.Role == ChatRole.User && !string.IsNullOrWhiteSpace(message.Text))
            .Select(message => new Mem0Message("user", message.Text!))
            .ToArray();

        if (messages.Length > 0)
        {
            await _mem0Client.AddMemories(messages, _userId, _appId, cancellationToken);
        }
    }

    [Description("Correct one existing user memory using the exact memory ID supplied in the relevant-memory context.")]
    private async Task<string> UpdateUserMemoryAsync(
        string memoryId,
        string newText,
        CancellationToken cancellationToken = default)
    {
        await EnsureOwnedByCurrentUserAsync(memoryId, cancellationToken);
        Mem0Memory memory = await _mem0Client.UpdateAsync(memoryId, newText, cancellationToken);
        _memoryWasMutated = true;
        return $"Updated memory {memory.Id}.";
    }

    [Description("Delete one existing user memory. Only call when the user explicitly asks to forget it, using the exact ID supplied in the relevant-memory context.")]
    private async Task<string> DeleteUserMemoryAsync(
        string memoryId,
        CancellationToken cancellationToken = default)
    {
        await EnsureOwnedByCurrentUserAsync(memoryId, cancellationToken);
        await _mem0Client.DeleteAsync(memoryId, cancellationToken);
        _memoryWasMutated = true;
        return $"Deleted memory {memoryId}.";
    }

    [Description("Delete every memory for the current user in this application. Only call when the user explicitly asks to forget everything and has confirmed the request.")]
    private async Task<string> DeleteAllUserMemoriesAsync(CancellationToken cancellationToken = default)
    {
        await _mem0Client.DeleteAllAsync(_userId, _appId, cancellationToken);
        _memoryWasMutated = true;
        return "Deleted all memories for the current user in this application.";
    }

    private bool MemoryWasMutatedByTool()
    {
        bool value = _memoryWasMutated;
        _memoryWasMutated = false;
        return value;
    }

    private async Task EnsureOwnedByCurrentUserAsync(string memoryId, CancellationToken cancellationToken)
    {
        Mem0Memory memory = await _mem0Client.GetAsync(memoryId, cancellationToken);
        if (memory.UserId != _userId || memory.AppId != _appId)
        {
            throw new InvalidOperationException("The memory does not belong to the current user and application.");
        }
    }
}
