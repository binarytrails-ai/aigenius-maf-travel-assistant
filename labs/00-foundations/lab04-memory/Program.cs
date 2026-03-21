// Lab 04: Long-Term Memory
// Learn how to persist user preferences across sessions using file storage

// Add NuGet package references
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
using System.Text.Json;

const string SourceName = "TravelAssistant";
const string ServiceName = "TravelAssistant";

// Configure JSON serialization for Azure SDK compatibility with .NET 10
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

// Step 1: Load environment variables
LoadEnv();

// Step 2: Initialize OpenTelemetry
var (loggerFactory, appLogger, tracerProvider) = InitTelemetry(ServiceName);

// Step 3: Create chat client
var chatClient = CreateChatClient(appLogger);
if (chatClient == null)
{
    tracerProvider.Dispose();
    return;
}

// Step 4: Create context provider with persistent storage
// Initialize with hardcoded preferences to demonstrate usage first
var userPreferences = new TravelPreferencesMemory(chatClient, "user123", appLogger, new UserTravelPreferences
{
    DestinationTypes = "beach destinations and cultural sites",
    TravelStyle = "budget-friendly with local experiences",
    SeatPreference = "window seat",
    MealPreference = "vegetarian meals"
});

// Step 5: Create agent with memory
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistant",
    ChatOptions = new()
    {
        Instructions = """
        You are a helpful travel assistant. 
        When making suggestions, naturally reference their preferences like:
        "Since you enjoy [preference], I recommend..."
        When you learn new preferences, acknowledge them warmly.
        Always tailor your advice to their stored preferences.
        """,
        Tools = []
    },
    AIContextProviders = [userPreferences]
});

agent.AsBuilder()
    .UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
    .UseLogging(loggerFactory)
    .Build();

appLogger.LogInformation("Agent created with memory successfully");

// Step 6: Run conversation with memory
try
{
    // First conversation - agent uses hardcoded preferences
    AgentSession session1 = await agent.CreateSessionAsync();

    var userInput1 = "I'm thinking about taking a vacation next month. Any suggestions?";
    appLogger.LogInformation("User: {UserInput}", userInput1);
    var response1 = await agent.RunAsync(userInput1, session1);
    appLogger.LogInformation("Agent: {AgentResponse}\n", response1.Text);

    // Second session - learn new preferences
    AgentSession session2 = await agent.CreateSessionAsync();

    var userInput2 = "Actually, I've gotten really interested in mountain hiking and adventure travel lately.";
    appLogger.LogInformation("User: {UserInput}", userInput2);
    var response2 = await agent.RunAsync(userInput2, session2);
    appLogger.LogInformation("Agent: {AgentResponse}\n", response2.Text);

    appLogger.LogInformation(new string('-', 80));

    // Continue in same session to see updated preferences in action
    AgentSession session3 = await agent.CreateSessionAsync();
    var userInput3 = "Can you recommend a trip for this summer?";
    appLogger.LogInformation("User: {UserInput}", userInput3);
    var response3 = await agent.RunAsync(userInput3, session3);
    appLogger.LogInformation("Agent: {AgentResponse}\n", response3.Text);

    appLogger.LogInformation(new string('=', 80));
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
    while (currentDir != null)
    {
        var azureYaml = Path.Combine(currentDir, "azure.yaml");
        if (File.Exists(azureYaml))
        {
            var envFile = Path.Combine(currentDir, ".env");
            if (File.Exists(envFile))
            {
                Env.Load(envFile);
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

// ==================== Models ====================

internal sealed class UserTravelPreferences
{
    public string? DestinationTypes { get; set; }
    public string? TravelStyle { get; set; }
    public string? SeatPreference { get; set; }
    public string? MealPreference { get; set; }
    public string? OtherPreferences { get; set; }
}

// ==================== Memory Provider ====================

internal sealed class TravelPreferencesMemory : AIContextProvider
{
    private readonly IChatClient _chatClient;
    private readonly string _userId;
    private readonly ILogger _logger;
    private UserTravelPreferences _preferences;

    public TravelPreferencesMemory(IChatClient chatClient, string userId, ILogger logger, UserTravelPreferences? initialPreferences = null) : base(null, null)
    {
        _chatClient = chatClient;
        _userId = userId;
        _logger = logger;

        // Initialize with provided preferences or create empty
        _preferences = initialPreferences ?? new UserTravelPreferences();
        _logger.LogInformation("Initialized memory for user: {UserId}", _userId);
    }

    public override string StateKey => "TravelPreferences";

    // Called BEFORE agent responds - inject stored preferences into context
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        var preferencesInfo = BuildPreferencesContext();

        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = preferencesInfo
        });
    }

    // Called AFTER agent responds - extract and persist new preferences
    protected override async ValueTask StoreAIContextAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        // Extract preferences from the conversation
        var extractedPreferences = await ExtractPreferencesAsync(context, cancellationToken);

        if (extractedPreferences != null)
        {
            // Update stored preferences
            bool updated = false;

            if (!string.IsNullOrEmpty(extractedPreferences.DestinationTypes))
            {
                _preferences.DestinationTypes = extractedPreferences.DestinationTypes;
                updated = true;
                _logger.LogInformation("Updated destination types: {Value}", extractedPreferences.DestinationTypes);
            }

            if (!string.IsNullOrEmpty(extractedPreferences.TravelStyle))
            {
                _preferences.TravelStyle = extractedPreferences.TravelStyle;
                updated = true;
                _logger.LogInformation("Updated travel style: {Value}", extractedPreferences.TravelStyle);
            }

            if (!string.IsNullOrEmpty(extractedPreferences.SeatPreference))
            {
                _preferences.SeatPreference = extractedPreferences.SeatPreference;
                updated = true;
                _logger.LogInformation("Updated seat preference: {Value}", extractedPreferences.SeatPreference);
            }

            if (!string.IsNullOrEmpty(extractedPreferences.MealPreference))
            {
                _preferences.MealPreference = extractedPreferences.MealPreference;
                updated = true;
                _logger.LogInformation("Updated meal preference: {Value}", extractedPreferences.MealPreference);
            }

            if (!string.IsNullOrEmpty(extractedPreferences.OtherPreferences))
            {
                _preferences.OtherPreferences = extractedPreferences.OtherPreferences;
                updated = true;
                _logger.LogInformation("Updated other preferences: {Value}", extractedPreferences.OtherPreferences);
            }

            if (updated)
            {
                _logger.LogInformation("Preferences updated in memory");
            }
        }
    }

    private string BuildPreferencesContext()
    {
        var hasPreferences = !string.IsNullOrEmpty(_preferences.DestinationTypes) ||
                           !string.IsNullOrEmpty(_preferences.TravelStyle) ||
                           !string.IsNullOrEmpty(_preferences.SeatPreference) ||
                           !string.IsNullOrEmpty(_preferences.MealPreference) ||
                           !string.IsNullOrEmpty(_preferences.OtherPreferences);

        if (!hasPreferences)
        {
            return "No travel preferences stored yet for this user.";
        }

        var context = "User's stored travel preferences:\n";

        if (!string.IsNullOrEmpty(_preferences.DestinationTypes))
            context += $"- Likes to visit: {_preferences.DestinationTypes}\n";

        if (!string.IsNullOrEmpty(_preferences.TravelStyle))
            context += $"- Travel style: {_preferences.TravelStyle}\n";

        if (!string.IsNullOrEmpty(_preferences.SeatPreference))
            context += $"- Seat preference: {_preferences.SeatPreference}\n";

        if (!string.IsNullOrEmpty(_preferences.MealPreference))
            context += $"- Meal preference: {_preferences.MealPreference}\n";

        if (!string.IsNullOrEmpty(_preferences.OtherPreferences))
            context += $"- Other: {_preferences.OtherPreferences}\n";

        return context;
    }

    private async Task<UserTravelPreferences?> ExtractPreferencesAsync(
        InvokedContext context,
        CancellationToken cancellationToken)
    {
        // Get recent conversation messages
        var recentMessages = context.RequestMessages.TakeLast(4).ToList();
        var conversationText = string.Join("\n",
            recentMessages.Select(m => $"{m.Role}: {string.Join(" ", m.Contents.Select(c => c.ToString()))}"));

        // Use LLM to extract preferences from conversation
        var extractionPrompt = @$"
Extract travel preferences from this conversation. Return only JSON with these fields:
- destinationTypes (types of places they like to visit: beaches, mountains, cities, cultural sites, etc.)
- travelStyle (how they like to travel: luxury, budget-friendly, adventure, relaxation, local experiences, etc.)
- seatPreference (e.g., 'window seat', 'aisle seat', 'front of plane')
- mealPreference (e.g., 'vegetarian', 'vegan', 'no peanuts')  
- otherPreferences (any other travel-related preferences)

Only include fields where preferences are explicitly stated. Return empty object {{}} if no preferences mentioned.

Conversation:
{conversationText}

Return valid JSON only.";

        var extractionMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, extractionPrompt)
        };

        try
        {
            var response = await _chatClient.GetResponseAsync<UserTravelPreferences>(
                extractionMessages,
                new ChatOptions
                {
                    ResponseFormat = ChatResponseFormat.Json
                },
                cancellationToken: cancellationToken);

            return response.Result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract preferences");
            return null;
        }
    }
}
