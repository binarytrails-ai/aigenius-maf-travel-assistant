// Lab 02: Agent with Context
// Learn how to provide additional context to agents using AIContextProvider

// Add NuGet package references
#:package Azure.AI.OpenAI@2.1.0
#:package Azure.Identity@1.21.0
#:package Microsoft.Agents.AI@1.1.0
#:package Microsoft.Agents.AI.Abstractions@1.1.0
#:package Microsoft.Extensions.AI@10.5.0
#:package Microsoft.Extensions.AI.OpenAI@10.5.0
#:package DotNetEnv@3.1.1
#:package OpenTelemetry@1.15.2
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.15.2
#:package OpenTelemetry.Extensions.Hosting@1.15.2
#:package Microsoft.Extensions.Logging@10.0.0
#:package Microsoft.Extensions.Logging.Console@10.0.0
#:package Microsoft.Extensions.DependencyInjection@10.0.0

using Azure.AI.OpenAI;
using Azure.Identity;
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

// Step 1: Load environment variables
LoadEnv();

// Step 2: Initialize OpenTelemetry
var (loggerFactory, appLogger, tracerProvider) = InitTelemetry(ServiceName);

// Step 3: Create chat client
var chatClient = CreateChatClient(appLogger);

// Step 4: Create context provider with travel knowledge
var travelContext = new TravelKnowledgeContext();

// Step 5: Create agent with context
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistant",
    ChatOptions = new()
    {
        Instructions = "You are a helpful travel assistant that provides travel recommendations and information. " +
                      "Be friendly, informative, and concise in your responses.",
        Tools = []
    },
    AIContextProviders = [travelContext]
});

agent.AsBuilder()
.UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
.UseLogging(loggerFactory)
.Build();

appLogger.LogInformation("Agent created successfully");

// Step 6: Run conversation
try
{
    AgentSession session = await agent.CreateSessionAsync();

    var userInput1 = "Can you recommend some travel destinations?";
    appLogger.LogInformation("User: {UserInput}", userInput1);

    var response1 = await agent.RunAsync(userInput1, session);
    appLogger.LogInformation("Agent: {AgentResponse}", response1.Text);

    // Second message - follow-up question to demonstrate multi-turn chat
    var userInput2 = "Which one would you recommend for families with kids?";
    appLogger.LogInformation("User: {UserInput}", userInput2);

    var response2 = await agent.RunAsync(userInput2, session);
    appLogger.LogInformation("Agent: {AgentResponse}", response2.Text);
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

IChatClient? CreateChatClient(ILogger appLogger)
{
    var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_ENDPOINT");
    var azureApiKey = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_KEY");
    var modelName = Environment.GetEnvironmentVariable("AZURE_TEXT_MODEL_NAME") ?? "gpt-4o";

    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    var githubModelId = Environment.GetEnvironmentVariable("GITHUB_TEXT_MODEL_ID") ?? "gpt-4o";
    var githubBaseUrl = Environment.GetEnvironmentVariable("GITHUB_MODELS_BASE_URL") ?? "https://models.inference.ai.azure.com";

    if (!string.IsNullOrEmpty(azureEndpoint) && !string.IsNullOrEmpty(azureApiKey))
    {
        appLogger.LogInformation("Using Azure OpenAI with model: {ModelName}", modelName);
        var azureClient = new AzureOpenAIClient(new Uri(azureEndpoint), new ApiKeyCredential(azureApiKey));
        return azureClient.GetChatClient(modelName)
            .AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
            .Build();
    }
    else if (!string.IsNullOrEmpty(githubToken))
    {
        appLogger.LogInformation("Using GitHub Models with model: {ModelId}", githubModelId);
        var githubClient = new AzureOpenAIClient(new Uri(githubBaseUrl), new ApiKeyCredential(githubToken));
        return githubClient.GetChatClient(githubModelId)
            .AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
            .Build();
    }
    else
    {
        appLogger.LogError("No valid credentials found.");
        return null;
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

// ==================== Context Provider ====================

internal sealed class TravelKnowledgeContext : AIContextProvider
{
    // Hard-coded travel knowledge that will be provided to the agent
    private const string TravelKnowledge = @"
DESTINATION HIGHLIGHTS:

## Adventure/Outdoors
| Destination | Location | Description |
|-------------|----------|-------------|
| Great Ocean Road | Victoria | Dramatic coastal cliffs and rainforest trails |
| Blue Mountains | NSW | World Heritage wilderness and hiking |
| Kakadu National Park | NT | Ancient landscapes and Indigenous culture |
| Tasmania Wilderness | Tasmania | Cradle Mountain, Overland Track |
| Queenstown | New Zealand | Adventure capital with bungee, hiking, skiing |

## Beaches/Coastal
| Destination | Location | Description |
|-------------|----------|-------------|
| Great Barrier Reef | Queensland | Vibrant coral reefs and tropical islands |
| Sunshine Coast | Queensland | Relaxed beaches and hinterland rainforests |
| Byron Bay | NSW | Iconic surf town with wellness culture |
| Margaret River | Western Australia | Stunning coastline with wine country |
| Coromandel Peninsula | New Zealand | White sand beaches and native bush |

## Wildlife
| Destination | Location | Description |
|-------------|----------|-------------|
| Kangaroo Island | South Australia | Wildlife sanctuary experiences |
| Phillip Island | Victoria | Penguin parades and coastal nature |
| Ningaloo Reef | Western Australia | Whale sharks and manta rays |
| Rottnest Island | Western Australia | Quokkas and marine life |
| Kaikoura | New Zealand | Whale watching and seal colonies |

## Cultural/Urban
| Destination | Location | Description |
|-------------|----------|-------------|
| Melbourne | Victoria | Arts, coffee culture, laneways |
| Sydney | NSW | Iconic harbor, beaches, diverse neighborhoods |
| Hobart | Tasmania | Heritage, MONA, food scene |
| Auckland | New Zealand | Polynesian culture meets modern city |
| Wellington | New Zealand | Creative capital with museums and cafes |

## Family
| Destination | Location | Description |
|-------------|----------|-------------|
| Gold Coast | Queensland | Theme parks and beaches |
| Port Douglas | Queensland | Reef access with family-friendly resorts |
| Canberra | ACT | Educational attractions and outdoor spaces |
| Bay of Islands | New Zealand | Dolphins, beaches, island hopping |

";

    public TravelKnowledgeContext() : base(null, null) { }

    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        // Provide the hard-coded travel knowledge to the agent
        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = "Use the following travel knowledge when answering questions:\n\n" + TravelKnowledge
        });
    }

    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
    }
}
