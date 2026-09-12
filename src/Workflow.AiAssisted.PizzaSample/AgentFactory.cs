
using Microsoft.Agents.AI;
using Shared;
using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace Workflow.AiAssisted.PizzaSample;

public class AgentFactory(Secrets secrets)
{
    public ChatClientAgent CreateOrderTakerAgent()
    {
        return ClientHelper.GetAzureOpenAIClient()
            .GetChatClient("gpt-4.1")
            .AsAIAgent(instructions: "You are a Pizza Order Taker, parsing the customers order");
    }

    public ChatClientAgent CreateWarningToCustomerAgent()
    {
        return ClientHelper.GetAzureOpenAIClient()
            .GetChatClient("gpt-4.1")
            .AsAIAgent(instructions: "You are a Pizza Confirmer. that need to explain to a user if a pizza order can't be met");
    }
}