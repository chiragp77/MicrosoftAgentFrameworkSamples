
using OpenAI;
using Shared;
using System.ClientModel;

Secrets secrets = Shared.SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(client);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

WebApplication app = builder.Build();

app.MapMcp("/mcp");

app.UseHttpsRedirection();

app.Run();