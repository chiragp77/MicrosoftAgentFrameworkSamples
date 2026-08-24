using Azure.AI.OpenAI;
using CommunityToolkit.VectorData.InMemory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Shared;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;

Utils.Init("Vector Store Deltas");

Secrets secrets = SecretsManager.GetSecrets();
using CustomClientHttpHandler handler = new();
using HttpClient httpClient = new(handler);

AzureOpenAIClient client = new(new Uri(secrets.AzureOpenAiEndpoint), new ApiKeyCredential(secrets.AzureOpenAiKey), new AzureOpenAIClientOptions
{
    Transport = new HttpClientPipelineTransport(httpClient)
});

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
    .GetEmbeddingClient("text-embedding-3-small")
    .AsIEmbeddingGenerator();

VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator
});

VectorStoreCollection<string, VectorRecord> vectorStoreCollection = vectorStore.GetCollection<string, VectorRecord>("knowledge_base");

//Data to import over time as it evolve

List<KnowledgeBaseEntry> v1Data =
[
    /*Add*/ new("What is the WI-FI Password at the Office?", "'Guest42'", ExternalId: "1"),
    /*Add*/ new("Is Christmas Eve a full or half day off", "Yes", ExternalId: "2"),
    /*Add*/ new("How do I register vacation?", "In portal", ExternalId: "3"),
];

List<KnowledgeBaseEntry> v2Data =
[
    /*Same*/ new("What is the WI-FI Password at the Office?", "'Guest42'", ExternalId: "1"),
    /*Same*/ new("Is Christmas Eve a full or half day off", "Yes", ExternalId: "2"),
    /*Same*/ new("How do I register vacation?", "In portal", ExternalId: "3"),
    /*Add*/ new("What do I need to do if I'm sick?", "Call Boss", ExternalId: "4"),
];

List<KnowledgeBaseEntry> v3Data =
[
    /*Change*/new("What is the WI-FI Password at the Office?", "'Guest43'", ExternalId: "1"),
    /*Delete*/ //Is Christmas Eve a full or half day off
    /*Same*/ new("How do I register vacation?", "In portal", ExternalId: "3"),
    /*Same*/ new("What do I need to do if I'm sick?", "Call Boss", ExternalId: "4"),
    /*Add*/ new("Where is the employee handbook?", "In Kitchen", ExternalId: "5"),
];

VectorStoreIngestionMethod method = VectorStoreIngestionMethod.DeltaWithRecordReuse;

Stopwatch stopwatch = Stopwatch.StartNew();
Console.WriteLine($"Ingestion Method: {method}");

Utils.Separator();

await Ingest(v1Data, "V1");
await ListStoreContent($"After V1 Data import of {v1Data.Count} records");

Utils.Separator();

await Ingest(v2Data, "V2");
await ListStoreContent($"After V2 Data import of {v2Data.Count} records");

Utils.Separator();

await Ingest(v3Data, "V3");
await ListStoreContent($"After V3 Data import of {v3Data.Count} records");

Console.WriteLine();
Utils.Yellow($"Total Time: {stopwatch.ElapsedMilliseconds} ms");

async Task Ingest(List<KnowledgeBaseEntry> data, string title)
{
    Utils.Green($"Ingest of {title}");

    switch (method)
    {
        case VectorStoreIngestionMethod.Incorrect:

            #region Incorrect

            {
                await vectorStoreCollection.EnsureCollectionExistsAsync();
                foreach (KnowledgeBaseEntry entry in data)
                {
                    await vectorStoreCollection.UpsertAsync(new VectorRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExternalId = entry.ExternalId,
                        Question = entry.Question,
                        Answer = entry.Answer,
                    });
                }
            }

            #endregion

            break;
        case VectorStoreIngestionMethod.ExternalIdAsId:

            #region ExternalIdAsId

            {
                await vectorStoreCollection.EnsureCollectionExistsAsync();
                foreach (KnowledgeBaseEntry entry in data)
                {
                    await vectorStoreCollection.UpsertAsync(new VectorRecord
                    {
                        Id = entry.ExternalId,
                        ExternalId = entry.ExternalId,
                        Question = entry.Question,
                        Answer = entry.Answer,
                    });
                }
            }

            #endregion

            break;
        case VectorStoreIngestionMethod.Inefficient:

            #region Inefficient

            {
                await vectorStoreCollection.EnsureCollectionDeletedAsync();
                await vectorStoreCollection.EnsureCollectionExistsAsync();
                foreach (KnowledgeBaseEntry entry in data)
                {
                    await vectorStoreCollection.UpsertAsync(new VectorRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExternalId = entry.ExternalId,
                        Question = entry.Question,
                        Answer = entry.Answer,
                    });
                }
            }

            #endregion

            break;
        case VectorStoreIngestionMethod.Delta:

            #region Delta

            {
                List<VectorRecord> allRecords = await GetAllVectorStoreRecords();
                List<string> activeIds = [];

                await vectorStoreCollection.EnsureCollectionExistsAsync();

                //Insert records that are new of changed
                foreach (KnowledgeBaseEntry entry in data)
                {
                    VectorRecord vectorRecordAboutToBeInserted = new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExternalId = entry.ExternalId,
                        Question = entry.Question,
                        Answer = entry.Answer,
                    };

                    VectorRecord? matchRecord = allRecords.FirstOrDefault(x => x.LookUpKey == vectorRecordAboutToBeInserted.LookUpKey);

                    if (matchRecord == null)
                    {
                        //New or Changed
                        await vectorStoreCollection.UpsertAsync(vectorRecordAboutToBeInserted);
                        activeIds.Add(vectorRecordAboutToBeInserted.Id);
                    }
                    else
                    {
                        //Unchanged; just record imported Ids
                        activeIds.Add(matchRecord.Id);
                    }
                }

                //Delete Records that are not more there
                List<string> idsToRemove = allRecords.Select(x => x.Id).Except(activeIds).ToList();
                await vectorStoreCollection.DeleteAsync(idsToRemove);
            }

            #endregion

            break;
        case VectorStoreIngestionMethod.DeltaWithRecordReuse:

            #region Delta (with record reuse)

            {
                List<VectorRecord> allRecords = await GetAllVectorStoreRecords();
                List<string> activeIds = [];

                await vectorStoreCollection.EnsureCollectionExistsAsync();

                //Insert records that are new of changed
                foreach (KnowledgeBaseEntry entry in data)
                {
                    VectorRecord vectorRecordAboutToBeInserted = new()
                    {
                        Id = entry.ExternalId, //Only difference from Delta scenario
                        ExternalId = entry.ExternalId,
                        Question = entry.Question,
                        Answer = entry.Answer,
                    };

                    VectorRecord? matchRecord = allRecords.FirstOrDefault(x => x.LookUpKey == vectorRecordAboutToBeInserted.LookUpKey);

                    if (matchRecord == null)
                    {
                        //New or Changed
                        await vectorStoreCollection.UpsertAsync(vectorRecordAboutToBeInserted);
                        activeIds.Add(vectorRecordAboutToBeInserted.Id);
                    }
                    else
                    {
                        //Unchanged; just record imported Ids
                        activeIds.Add(matchRecord.Id);
                    }
                }

                //Delete Records that are not more there
                List<string> idsToRemove = allRecords.Select(x => x.Id).Except(activeIds).ToList();
                await vectorStoreCollection.DeleteAsync(idsToRemove);
            }

            #endregion

            break;
    }
    Console.WriteLine("Embedding complete..");
}

async Task ListStoreContent(string title)
{
    List<VectorRecord> records = await GetAllVectorStoreRecords();
    Utils.Red($"{title} (There are {records.Count} records in the Vector-store)");
    foreach (VectorRecord record in records.OrderBy(x => x.Question))
    {
        Utils.Gray($"Q: {record.Question} | A: {record.Answer} (Id: {record.Id})");
    }
}

async Task<List<VectorRecord>> GetAllVectorStoreRecords()
{
    await vectorStoreCollection.EnsureCollectionExistsAsync();
    List<VectorRecord> allRecords = [];
    await foreach (VectorRecord vectorRecord in vectorStoreCollection.GetAsync(
                       filter: record => true,
                       top: int.MaxValue,
                       options: new FilteredRecordRetrievalOptions<VectorRecord>
                       {
                           IncludeVectors = false
                       }))
    {
        allRecords.Add(vectorRecord);
    }
    return allRecords;
}

public enum VectorStoreIngestionMethod
{
    Incorrect,
    ExternalIdAsId,
    Inefficient,
    Delta,
    DeltaWithRecordReuse
}

public record KnowledgeBaseEntry(string Question, string Answer, string ExternalId);

public class VectorRecord
{
    [VectorStoreKey]
    public required string Id { get; set; }

    [VectorStoreData]
    public required string Question { get; set; }

    [VectorStoreData]
    public required string ExternalId { get; set; }

    [VectorStoreData]
    public required string Answer { get; set; }

    [VectorStoreVector(1536)]
    public string Vector => $"Q: {Question} - A: {Answer}";

    public string LookUpKey => $"{ExternalId}___{Question}___{Answer}"; //Not the same as Vector as it include extra metadata
}

public class CustomClientHttpHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Utils.Yellow("Call to online Embedding Service 💸");
        return await base.SendAsync(request, cancellationToken);
    }
}