// Lab 01: Basic Agent
// Learn how to create and run a simple AI agent

// Add NuGet package references using file-based app syntax (#:package Name@Version)
#:package Azure.AI.OpenAI@2.1.0
#:package Azure.Identity@1.18.0
#:package Microsoft.Agents.AI@1.0.0-rc2
#:package Microsoft.Agents.AI.Abstractions@1.0.0-rc2
#:package Microsoft.Extensions.AI@10.3.0
#:package Microsoft.Extensions.AI.OpenAI@10.3.0
#:package DotNetEnv@3.1.1
#:package OpenTelemetry@1.10.0
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.10.0
#:package OpenTelemetry.Extensions.Hosting@1.10.0
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

// Step 1: Load environment variables from the workspace root .env file
LoadEnv();

// Step 2: Initialize OpenTelemetry
var (loggerFactory, appLogger, tracerProvider) = InitTelemetry(ServiceName);

// Step 3: Create chat client
var chatClient = CreateChatClient(appLogger);
if (chatClient == null) return;

// Step 4: Create a simple agent
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistant",
    ChatOptions = new()
    {
        Instructions = "You are a helpful travel assistant that provides travel recommendations and information. " +
                      "Be friendly, informative, and concise in your responses.",
        Tools = []
    }
});

agent.AsBuilder()
.UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
.UseLogging(loggerFactory)
.Build();

// Step 5: Run the agent with multi-turn conversation
try
{
    // Create a new session for the conversation
    AgentSession session = await agent.CreateSessionAsync();

    // First message
    var userInput1 = "What are some must-visit places in Australia?";
    appLogger.LogInformation("User: {UserInput}", userInput1);

    var response1 = await agent.RunAsync(userInput1, session);
    appLogger.LogInformation("Agent: {AgentResponse}", response1);

    // Second message - follow-up question to demonstrate multi-turn chat
    var userInput2 = "Which one would you recommend for families with kids?";
    appLogger.LogInformation("User: {UserInput}", userInput2);

    var response2 = await agent.RunAsync(userInput2, session);
    appLogger.LogInformation("Agent: {AgentResponse}", response2);
    appLogger.LogInformation("Agent response completed");

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
    // Configure OpenTelemetry for Aspire dashboard
    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

    // Setup tracing with OpenTelemetry
    var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: "1.0.0"))
        .AddSource(SourceName)
        .AddSource("Microsoft.Agents.AI")
        .AddSource("Microsoft.Extensions.AI")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
        .Build();

    // Setup structured logging with OpenTelemetry
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