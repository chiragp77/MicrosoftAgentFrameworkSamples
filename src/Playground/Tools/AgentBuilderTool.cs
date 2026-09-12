using System.ComponentModel;

using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;

namespace Playground.Tools;

public class AgentBuilderTool
{
    public async Task<string> RunSubAgent([Description("Give the agent a funny name")]string agentName, string systemInstructions, string prompt)
    {
        Utils.Green($"Sub-agent '{agentName}': Instructions: '{systemInstructions}' - Prompt: '{prompt}'");

        OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
        ChatClientAgent agent = client.GetChatClient("gpt-4.1-mini").AsAIAgent(instructions: systemInstructions);
        return (await agent.RunAsync(prompt)).ToString();
    }
}