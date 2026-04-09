// Lab 03: Travel Assistant with RAG (Retrieval Augmented Generation)
// Learn how to use TextSearchProvider with Microsoft Agent Framework to add RAG capabilities

// This sample shows how to build a travel visa assistant using retrieval augmented generation (RAG).
// The TextSearchProvider searches visa policy documents before each model invocation and injects the relevant information into the model context.
// This uses a simple in-memory vector store with embeddings and cosine similarity for semantic search.

// Add NuGet package references
#:package Azure.AI.OpenAI@2.1.0
#:package Azure.Identity@1.20.0
#:package Microsoft.Agents.AI@1.0.0
#:package Microsoft.Agents.AI.Abstractions@1.0.0
#:package Microsoft.Extensions.AI@10.4.1
#:package Microsoft.Extensions.AI.OpenAI@10.4.1
#:package DotNetEnv@3.1.1
#:package OpenTelemetry@1.15.1
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.15.1
#:package OpenTelemetry.Extensions.Hosting@1.15.1
#:package Microsoft.Extensions.Logging@10.0.0
#:package Microsoft.Extensions.Logging.Console@10.0.0
#:package Microsoft.Extensions.DependencyInjection@10.0.0

using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ClientModel;

const string SourceName = "TravelAssistant";
const string ServiceName = "TravelAssistant";

// Configure JSON serialization for Azure SDK compatibility with .NET 10
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

// Step 1: Load environment variables
LoadEnv();

// Step 2: Initialize OpenTelemetry
var (loggerFactory, appLogger, tracerProvider) = InitTelemetry(ServiceName);

// Step 3: Create chat client and embedding generator
var (chatClient, embeddingGenerator) = CreateClients(appLogger);
if (chatClient == null || embeddingGenerator == null)
{
    tracerProvider.Dispose();
    return;
}

// Step 4: Load visa policy documents into vector store
var workspaceRoot = Directory.GetCurrentDirectory();
var japanVisaPolicyPath = Path.Combine(workspaceRoot, "data", "visa-policy-japan.md");
var canadaVisaPolicyPath = Path.Combine(workspaceRoot, "data", "visa-policy-canada.md");

TextSearchStore textSearchStore = new(embeddingGenerator);
await UploadDocumentationFromFileAsync(japanVisaPolicyPath, "Japan Visa Policy", textSearchStore, 2000, 200, appLogger);
await UploadDocumentationFromFileAsync(canadaVisaPolicyPath, "Canada Visa Policy", textSearchStore, 2000, 200, appLogger);

// Step 5: Create search adapter for TextSearchProvider
Func<string, CancellationToken, Task<IEnumerable<TextSearchProvider.TextSearchResult>>> SearchAdapter = async (text, ct) =>
{
    var searchResults = await textSearchStore.SearchAsync(text, 5, ct);
    return searchResults.Select(r => new TextSearchProvider.TextSearchResult
    {
        SourceName = r.SourceName,
        SourceLink = r.SourceLink,
        Text = r.Text ?? string.Empty,
        RawRepresentation = r
    });
};

// Step 6: Configure TextSearchProvider options
TextSearchProviderOptions textSearchOptions = new()
{
    // Run the search prior to every model invocation
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
    // Use up to 5 recent messages when searching so that searches
    // still produce valuable results even when the user is referring
    // back to previous messages in their request
    RecentMessageMemoryLimit = 5
};

// Step 7: Create agent with RAG capabilities
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistant",
    ChatOptions = new()
    {
        Instructions = "You are a helpful travel assistant. Use the provided travel knowledge to give accurate recommendations and cite the source document when available.",
        Tools = []
    },
    AIContextProviders = [new TextSearchProvider(SearchAdapter, textSearchOptions)]
});

agent.AsBuilder()
.UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
.UseLogging(loggerFactory)
.Build();

appLogger.LogInformation("Agent created successfully");

// Step 8: Run conversation
try
{
    AgentSession session = await agent.CreateSessionAsync();

    var userInput1 = "Do I need a visa to visit Japan if I'm a Australian citizen?";
    appLogger.LogInformation("User: {UserInput}", userInput1);

    var response1 = await agent.RunAsync(userInput1, session);
    appLogger.LogInformation("Agent: {AgentResponse}", response1.Text);

}
catch (Exception ex)
{
    appLogger.LogError(ex, "Agent interaction failed: {ErrorMessage}", ex.Message);
}
finally
{
    tracerProvider.Dispose();
}

// ==================== Helper Methods ====================

(IChatClient?, IEmbeddingGenerator<string, Embedding<float>>?) CreateClients(ILogger appLogger)
{
    var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_ENDPOINT");
    var azureApiKey = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_KEY");
    var modelName = Environment.GetEnvironmentVariable("AZURE_TEXT_MODEL_NAME") ?? "gpt-4o";
    var embeddingModelName = Environment.GetEnvironmentVariable("AZURE_EMBEDDING_MODEL_NAME") ?? "text-embedding-ada-002";

    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    var githubModelId = Environment.GetEnvironmentVariable("GITHUB_TEXT_MODEL_ID") ?? "gpt-4o";
    var githubEmbeddingModelId = Environment.GetEnvironmentVariable("GITHUB_EMBEDDING_MODEL_ID") ?? "text-embedding-ada-002";
    var githubBaseUrl = Environment.GetEnvironmentVariable("GITHUB_MODELS_BASE_URL") ?? "https://models.inference.ai.azure.com";

    if (!string.IsNullOrEmpty(azureEndpoint) && !string.IsNullOrEmpty(azureApiKey))
    {
        appLogger.LogInformation("Using Azure OpenAI with model: {ModelName}", modelName);
        var azureClient = new AzureOpenAIClient(new Uri(azureEndpoint), new ApiKeyCredential(azureApiKey));
        return (
            azureClient.GetChatClient(modelName)
                .AsIChatClient()
                .AsBuilder()
                .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
                .Build(),
            azureClient.GetEmbeddingClient(embeddingModelName).AsIEmbeddingGenerator()
        );
    }
    else if (!string.IsNullOrEmpty(githubToken))
    {
        appLogger.LogInformation("Using GitHub Models with model: {ModelId}", githubModelId);
        var githubClient = new AzureOpenAIClient(new Uri(githubBaseUrl), new ApiKeyCredential(githubToken));
        return (
            githubClient.GetChatClient(githubModelId)
                .AsIChatClient()
                .AsBuilder()
                .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
                .Build(),
            githubClient.GetEmbeddingClient(githubEmbeddingModelId).AsIEmbeddingGenerator()
        );
    }
    else
    {
        appLogger.LogError("No API credentials found");
        return (null, null);
    }
}

void LoadEnv()
{
    var currentDir = Directory.GetCurrentDirectory();
    for (int i = 0; i < 10 && currentDir != null; i++)
    {
        var azureYamlPath = Path.Combine(currentDir, "azure.yaml");
        if (File.Exists(azureYamlPath))
        {
            var envPath = Path.Combine(currentDir, ".env");
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                return;
            }
        }
        currentDir = Directory.GetParent(currentDir)?.FullName;
    }
}

(ILoggerFactory, ILogger<Program>, TracerProvider) InitTelemetry(string serviceName)
{
    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

    var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: "1.0.0"))
        .AddSource(SourceName)
        .AddSource("Microsoft.Agents.AI")
        .AddSource("Microsoft.Extensions.AI")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
        .Build();

    var serviceCollection = new ServiceCollection();
    serviceCollection.AddLogging(loggingBuilder => loggingBuilder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole()
        .AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion: "1.0.0"));
            options.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri(otlpEndpoint));
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        }));

    var serviceProvider = serviceCollection.BuildServiceProvider();
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var appLogger = loggerFactory.CreateLogger<Program>();

    return (loggerFactory, appLogger, tracerProvider);
}

static async Task UploadDocumentationFromFileAsync(
    string filePath,
    string sourceName,
    TextSearchStore store,
    int chunkSize,
    int overlap,
    ILogger appLogger)
{
    if (!File.Exists(filePath))
    {
        appLogger.LogError("File not found: {FilePath}", filePath);
        return;
    }

    var markdown = await File.ReadAllTextAsync(filePath);
    var documents = new List<TextSearchDocument>();
    for (int i = 0; i < markdown.Length; i += chunkSize)
    {
        var text = markdown.Substring(i, Math.Min(chunkSize + overlap, markdown.Length - i));

        documents.Add(new TextSearchDocument
        {
            SourceId = $"{sourceName}-chunk-{documents.Count}",
            SourceName = sourceName,
            SourceLink = filePath,
            Text = text
        });
    }

    await store.UpsertDocumentsAsync(documents);
}

// ==================== Helper Classes ====================

// Simple document model for text search
public class TextSearchDocument
{
    public string SourceId { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceLink { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public ReadOnlyMemory<float> Embedding { get; set; }
}

// A simple in-memory store that uses embeddings for semantic search
public class TextSearchStore
{
    private readonly List<TextSearchDocument> _documents = new();
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public TextSearchStore(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task UpsertDocumentsAsync(IEnumerable<TextSearchDocument> documents)
    {
        foreach (var doc in documents)
        {
            // Generate embedding for the document text
            var embeddingResult = await _embeddingGenerator.GenerateAsync(doc.Text);
            doc.Embedding = embeddingResult.Vector;
            _documents.Add(doc);
        }
    }

    public Task<IEnumerable<TextSearchDocument>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default)
    {
        // For this simplified version, generate embedding for the query and find closest matches
        return SearchWithEmbeddingAsync(query, topK, cancellationToken);
    }

    private async Task<IEnumerable<TextSearchDocument>> SearchWithEmbeddingAsync(string query, int topK, CancellationToken cancellationToken)
    {
        if (_documents.Count == 0)
            return Enumerable.Empty<TextSearchDocument>();

        // Generate embedding for the query
        var queryEmbeddingResult = await _embeddingGenerator.GenerateAsync(query, cancellationToken: cancellationToken);
        var queryEmbedding = queryEmbeddingResult.Vector;

        // Calculate cosine similarity for each document and return top K
        var results = _documents
            .Select(doc => new
            {
                Document = doc,
                Score = CosineSimilarity(queryEmbedding.Span, doc.Embedding.Span)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Document);

        return results;
    }

    private static float CosineSimilarity(ReadOnlySpan<float> vector1, ReadOnlySpan<float> vector2)
    {
        float dot = 0, mag1 = 0, mag2 = 0;
        for (int i = 0; i < vector1.Length; i++)
        {
            dot += vector1[i] * vector2[i];
            mag1 += vector1[i] * vector1[i];
            mag2 += vector2[i] * vector2[i];
        }
        return dot / (MathF.Sqrt(mag1) * MathF.Sqrt(mag2));
    }
}
