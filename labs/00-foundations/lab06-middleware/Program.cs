// Lab 06: Middleware
// Learn how to implement PII filtering middleware with Azure OpenAI
// The middleware automatically redacts sensitive personal information like
// phone numbers, email addresses, and names from both input and output messages.

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

using System.ClientModel;
using System.Text.RegularExpressions;
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

const string SourceName = "TravelAssistant";
const string ServiceName = "TravelAssistant";

// Step 1: Load environment variables from the workspace root .env file
LoadEnv();

// Step 2: Initialize OpenTelemetry
var (loggerFactory, appLogger, tracerProvider) = InitTelemetry(ServiceName);

// Step 3: Create chat client
var chatClient = CreateChatClient(appLogger);
if (chatClient == null) return;

// Step 4: Create an agent with PII filtering middleware
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistantWithPII",
    ChatOptions = new()
    {
        Instructions = "You are a helpful AI assistant that helps people find information.",
        Tools = []
    }
})
.AsBuilder()
.Use(PIIMiddleware, null)
.UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
.UseLogging(loggerFactory)
.Build();

// Step 5: Run the agent with PII filtering
try
{
    var session = await agent.CreateSessionAsync();

    appLogger.LogInformation("=== PII Detection and Redaction ===");
    appLogger.LogInformation("");

    appLogger.LogInformation("Example 1: Filtering PII in user input");
    var userInput1 = "My name is John Doe. Here is my contact info: 123-456-7890, john@example.com";
    appLogger.LogInformation("INPUT: {UserInput}", userInput1);

    var response1 = await agent.RunAsync(userInput1, session);
    appLogger.LogInformation("OUTPUT: {AgentResponse}", response1);

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


// ==================== Middleware ====================

// ==================== Middleware ====================

// This middleware redacts PII information from input and output messages.
async Task<AgentResponse> PIIMiddleware(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
{
    // Redact PII information from input messages
    var filteredMessages = FilterMessages(messages);
    appLogger.LogInformation("PII Middleware - Filtering Messages Pre-Run");

    var response = await innerAgent.RunAsync(filteredMessages, session, options, cancellationToken).ConfigureAwait(false);

    // Redact PII information from output messages
    response.Messages = FilterMessages(response.Messages);

    appLogger.LogInformation("PII Middleware - Filtering Messages Post-Run");

    return response;

    static IList<ChatMessage> FilterMessages(IEnumerable<ChatMessage> messages)
    {
        return messages.Select(m => new ChatMessage(m.Role, FilterPii(m.Text))).ToList();
    }

    static string FilterPii(string content)
    {
        // Regex patterns for PII detection (simplified for demonstration)
        Regex[] piiPatterns =
        [
            new(@"\b\d{3}-\d{3}-\d{4}\b", RegexOptions.Compiled), // Phone number (e.g., 123-456-7890)
            new(@"\b[\w\.-]+@[\w\.-]+\.\w+\b", RegexOptions.Compiled), // Email address
            new(@"\b[A-Z][a-z]+\s[A-Z][a-z]+\b", RegexOptions.Compiled) // Full name (e.g., John Doe)
        ];

        foreach (var pattern in piiPatterns)
        {
            content = pattern.Replace(content, "[REDACTED: PII]");
        }

        return content;
    }
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
