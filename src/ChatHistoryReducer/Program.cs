//Video related to this sample: https://youtu.be/a-7wyjpf5qQ


using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

#pragma warning disable MEAI001

Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
ChatClient chatClient = client.GetChatClient("gpt-4.1");

IChatReducer chatReducer = new MessageCountingChatReducer(targetCount: 4);
IChatReducer chatReducer2 = new SummarizingChatReducer(chatClient.AsIChatClient(), targetCount: 1, threshold: 4);

ChatClientAgent agent = client
    .GetChatClient("gpt-4.1")
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new()
        {
            Instructions = "You are a Friendly AI Bot, answering questions",
        },
        ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
        {
            ChatReducer = chatReducer2,
        })
    });

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? string.Empty;
    AgentResponse response = await agent.RunAsync(input, session);
    Console.WriteLine(response);
    response.Usage.OutputAsInformation();

    InMemoryChatHistoryProvider? provider = agent.GetService<InMemoryChatHistoryProvider>();
    List<ChatMessage> messagesInSession = provider?.GetMessages(session) ?? [];
    Utils.Gray("- Number of messages in session: " + messagesInSession.Count());
    foreach (ChatMessage message in messagesInSession)
    {
        Utils.Gray($"-- {message.Role}: {message.Text}");
    }
    Utils.Separator();
}