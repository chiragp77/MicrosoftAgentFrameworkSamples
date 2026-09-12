
using OpenAI;
using Shared;
using System.ClientModel;
using The_Trello_Experiment;

Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
//await Take1OpenAICodeInterpreter.Run(client, secrets.TrelloApiKey, secrets.TrelloToken);
//await Take2CSharpCodeRunner.Run(client, secrets.TrelloApiKey, secrets.TrelloToken);
await Take3TheSensibleChoice.Run(client, secrets.TrelloApiKey, secrets.TrelloToken);