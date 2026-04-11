// Add NuGet package references
#:package Azure.AI.OpenAI@2.1.0
#:package DotNetEnv@3.1.1
#:package Microsoft.Azure.Cosmos@3.43.1
#:package Newtonsoft.Json@13.0.3

using Azure;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using OpenAI.Embeddings;

try
{
    // Enable reflection-based JSON serialization for OpenAI SDK
    AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

    LoadEnv();

    var cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT")!;
    var databaseName = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE_NAME")!;
    var chatContainer = Environment.GetEnvironmentVariable("COSMOS_DB_CHAT_HISTORY_CONTAINER")!;
    var flightsContainer = Environment.GetEnvironmentVariable("COSMOS_DB_FLIGHTS_CONTAINER") ?? "Flights";
    var azureAIEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_SERVICE_ENDPOINT")!;
    var resourceGroup = Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP")!;

    var cosmosClient = CreateCosmosClient(cosmosEndpoint);
    var embeddingModelName = Environment.GetEnvironmentVariable("AZURE_EMBEDDING_MODEL_NAME") ?? "text-embedding-ada-002";
    var azureAIKey = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_KEY");

    AzureOpenAIClient azureOpenAIClient;
    azureOpenAIClient = new AzureOpenAIClient(new Uri(azureAIEndpoint), new AzureKeyCredential(azureAIKey));

    var embeddingClient = azureOpenAIClient.GetEmbeddingClient(embeddingModelName);

    Console.WriteLine($"Using embedding model: {embeddingModelName}\n");

    var rootDir = GetRootDirectory();

    await SeedChatHistoryAsync(cosmosClient, databaseName, chatContainer, embeddingClient, rootDir);

    await SeedFlightsAsync(cosmosClient, databaseName, flightsContainer, embeddingClient, rootDir);

    Console.WriteLine("\n=== Seeding Complete ===");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"\nERROR: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}


// ==================== Helper Methods ====================

CosmosClient CreateCosmosClient(string cosmosEndpoint)
{
    Console.WriteLine("Connecting to Cosmos DB...");

    // Configure client options for better seeding performance
    var options = new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        RequestTimeout = TimeSpan.FromSeconds(60),
        MaxRetryAttemptsOnRateLimitedRequests = 9,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(60),
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    };

    // Priority 1: Connection string from environment
    var cosmosConnectionString = Environment.GetEnvironmentVariable("COSMOS_DB_CONNECTION_STRING");
    if (!string.IsNullOrEmpty(cosmosConnectionString))
    {
        Console.WriteLine("  Using connection string from environment\n");
        return new CosmosClient(cosmosConnectionString, options);
    }

    // Priority 2: Key from environment
    var cosmosKey = Environment.GetEnvironmentVariable("COSMOS_DB_KEY");
    if (!string.IsNullOrEmpty(cosmosKey))
    {
        Console.WriteLine("  Using key from environment\n");
        return new CosmosClient(cosmosEndpoint, cosmosKey, options);
    }

    throw new InvalidOperationException("Either COSMOS_DB_CONNECTION_STRING or COSMOS_DB_KEY environment variable is required");
}

async Task SeedChatHistoryAsync(
    CosmosClient cosmosClient,
    string databaseName,
    string containerName,
    EmbeddingClient embeddingClient,
    string rootDir)
{
    Console.WriteLine("--- Seeding Chat History ---");

    var container = cosmosClient.GetContainer(databaseName, containerName);
    var chatHistoryPath = Path.Combine(rootDir, "data", "chat_history.json");

    if (!File.Exists(chatHistoryPath))
    {
        Console.WriteLine($"  SKIPPED: chat_history.json not found at {chatHistoryPath}\n");
        return;
    }

    var chatHistoryJson = await File.ReadAllTextAsync(chatHistoryPath);
    var chatHistory = JArray.Parse(chatHistoryJson);

    Console.WriteLine($"  Loaded {chatHistory.Count} records from chat_history.json");

    var inserted = 0;
    var existing = 0;

    foreach (JObject item in chatHistory)
    {
        var content = item["Content"]?.ToString() ?? "";

        // Generate embedding (with delay to avoid rate limiting)
        await Task.Delay(200);
        var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(content);
        var embedding = embeddingResponse.Value.ToFloats().ToArray();

        var record = new JObject
        {
            ["id"] = item["id"],
            ["ApplicationId"] = item["ApplicationId"],
            ["UserId"] = item["UserId"],
            ["ThreadId"] = item["ThreadId"],
            ["Role"] = item["Role"],
            ["Content"] = content,
            ["ContentEmbedding"] = new JArray(embedding),
            ["CreatedAt"] = DateTime.UtcNow
                .AddDays(-(int)(item["DaysAgo"] ?? 0))
                .AddMinutes((int)(item["MinutesOffset"] ?? 0)),
            ["_ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        try
        {
            await container.CreateItemAsync(record, new PartitionKey(record["ApplicationId"]!.ToString()));
            inserted++;

            if (inserted % 5 == 0)
            {
                Console.WriteLine($"  Progress: {inserted}/{chatHistory.Count} inserted...");
            }
            
            // Small delay between writes to avoid throttling
            await Task.Delay(100);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            existing++;
        }
    }

    Console.WriteLine($"  Summary: {inserted} inserted, {existing} already existed\n");
}

async Task SeedFlightsAsync(
    CosmosClient cosmosClient,
    string databaseName,
    string containerName,
    EmbeddingClient embeddingClient,
    string rootDir)
{
    Console.WriteLine("--- Seeding Flights Data ---");

    var container = cosmosClient.GetContainer(databaseName, containerName);
    var flightsPath = Path.Combine(rootDir, "data", "flights_data.json");

    if (!File.Exists(flightsPath))
    {
        Console.WriteLine($"  SKIPPED: flights_data.json not found at {flightsPath}\n");
        return;
    }

    var flightsJson = await File.ReadAllTextAsync(flightsPath);
    var flights = JArray.Parse(flightsJson);

    Console.WriteLine($"  Loaded {flights.Count} flight records");

    var inserted = 0;
    var existing = 0;
    var vectorized = 0;

    foreach (JObject flight in flights)
    {
        // Generate vector embedding for flightProfile if it exists
        if (flight["flightProfile"] != null)
        {
            var flightProfile = flight["flightProfile"]!.ToString();
            
            // Add delay to avoid rate limiting
            await Task.Delay(200);
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(flightProfile);
            var embedding = embeddingResponse.Value.ToFloats().ToArray();

            flight["flightProfileVector"] = new JArray(embedding);
            vectorized++;
        }

        try
        {
            await container.CreateItemAsync(flight, new PartitionKey(flight["id"]!.ToString()));
            inserted++;

            if (inserted % 5 == 0)
            {
                Console.WriteLine($"  Progress: {inserted}/{flights.Count} inserted...");
            }
            
            // Small delay between writes to avoid throttling
            await Task.Delay(100);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            existing++;
        }
    }

    Console.WriteLine($"  Summary: {inserted} inserted, {existing} already existed, {vectorized} vectorized\n");
}

string GetRootDirectory()
{
    // Navigate from scripts/seed-cosmosdb to workspace root
    var currentDir = Directory.GetCurrentDirectory();
    for (int i = 0; i < 10 && currentDir != null; i++)
    {
        var azureYamlPath = Path.Combine(currentDir, "azure.yaml");
        if (File.Exists(azureYamlPath))
        {
            return currentDir;
        }
        currentDir = Directory.GetParent(currentDir)?.FullName;
    }

    // Fallback: assume we're in scripts/seed-cosmosdb
    return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));
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
                Console.WriteLine($"Loaded environment from: {envPath}");
                return;
            }
        }
        currentDir = Directory.GetParent(currentDir)?.FullName;
    }

    Console.WriteLine("Warning: No .env file found");
}
