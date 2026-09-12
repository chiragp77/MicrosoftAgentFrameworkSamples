using AgentSkills;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;

#pragma warning disable MAAI001
Utils.Init("AI Context Provider (AgentSkills)");
OpenAIClient client = ClientHelper.GetAzureOpenAIClient(showRawCall: true);

string skillPath = "TestData\\AgentSkills";

AIAgent agent = client.GetChatClient("gpt-4.1-mini").AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [new AgentSkillsProvider(skillPath)],
    ChatOptions = new ChatOptions
    {
        Tools = [AIFunctionFactory.Create(PythonRunner.RunPhytonScript, name: "execute_python")]
    }
}).AsBuilder().Use(Utils.ToolCallingMiddleware).Build();


await Utils.RunChatLoopWithSession(agent);