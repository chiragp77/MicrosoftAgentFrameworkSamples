
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

namespace DependencyInjection.Components.Pages;

public partial class Home(OpenAIClient OpenAIClient, [FromKeyedServices("gpt-4.1")] ChatClient chatClient, [FromKeyedServices("gpt-4.1")] ChatClientAgent agentInjected)
{
    private string? _question;
    private string? _answer;

    private async Task AskAi()
    {
        if (string.IsNullOrWhiteSpace(_question))
        {
            return;
        }

        ChatClientAgent agent = OpenAIClient
            .GetChatClient("gpt-4.1")
            .AsAIAgent();

        //Alternatives
        //agent = chatClient.AsAIAgent();
        //agent = agentInjected;

        AgentResponse response = await agent.RunAsync(_question);
        _answer = response.Text;
        StateHasChanged();
    }
}