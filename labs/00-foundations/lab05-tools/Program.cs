// Lab 03: Tools and Function Calling
// Learn how to equip agents with tools that they can call automatically

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

using System.ComponentModel;
using System.Text.Json;
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

// Step 4: Define tools that the agent can use
var tools = new List<AITool>
{
    AIFunctionFactory.Create(GetCurrentDate),
    AIFunctionFactory.Create(CalculateDateDifference),
    AIFunctionFactory.Create(CalculateDaysUntil),
    AIFunctionFactory.Create(CalculateTimeZone)
};

appLogger.LogInformation("Created {ToolCount} tools for the agent", tools.Count);

// Step 5: Create agent with tools
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistant",
    ChatOptions = new()
    {
        Instructions = """
            You are a helpful travel planning assistant with tools for dates and Australian time zones.
            
            Use the tools to answer questions.
            Provide friendly, conversational responses based on the tool results.
            """,
        Tools = tools
    }
});

agent.AsBuilder()
.UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
.UseLogging(loggerFactory)
.Build();

appLogger.LogInformation("Agent created with tools successfully");

// Step 6: Run conversation with tool usage
try
{
    // Example 1: Simple time zone difference
    var userInput2 = "What's the time difference between Melbourne and Brisbane?";
    appLogger.LogInformation("User: {UserInput}", userInput2);
    var response2 = await agent.RunAsync(userInput2);
    appLogger.LogInformation("Agent: {AgentResponse}", response2.Text);
    Console.WriteLine();

    // Example 2: Time zone calculation between Australian cities
    var userInput1 = "What time is it in Perth when it's 3:00 PM in Sydney?";
    appLogger.LogInformation("User: {UserInput}", userInput1);
    var response1 = await agent.RunAsync(userInput1);
    appLogger.LogInformation("Agent: {AgentResponse}", response1.Text);
    Console.WriteLine();

}
catch (Exception ex)
{
    appLogger.LogError(ex, "Agent interaction failed: {ErrorMessage}", ex.Message);
}
finally
{
    tracerProvider.Dispose();
}


// ==================== Tool Definitions ====================

/// <summary>
/// Gets the current date.
/// </summary>
[Description("Get today's date. Use this when you need to know what today's date is for calculations or comparisons.")]
static string GetCurrentDate()
{
    var today = DateOnly.FromDateTime(DateTime.Now);
    var result = new
    {
        date = today.ToString("yyyy-MM-dd"),
        dayOfWeek = today.DayOfWeek.ToString()
    };

    return JsonSerializer.Serialize(result);
}

/// <summary>
/// Calculates days between two dates.
/// </summary>
[Description("Calculate how many days between two dates. Dates must be in YYYY-MM-DD format.")]
static string CalculateDateDifference(
    [Description("Start date in YYYY-MM-DD format")] string startDate,
    [Description("End date in YYYY-MM-DD format")] string endDate)
{
    var start = DateOnly.Parse(startDate);
    var end = DateOnly.Parse(endDate);
    var days = end.DayNumber - start.DayNumber;

    var result = new
    {
        startDate,
        endDate,
        totalDays = days
    };

    return JsonSerializer.Serialize(result);
}

/// <summary>
/// Calculates days until a future date.
/// </summary>
[Description("Calculate how many days from today until a future date. Date must be in YYYY-MM-DD format.")]
static string CalculateDaysUntil(
    [Description("Target date in YYYY-MM-DD format")] string targetDate)
{
    var today = DateOnly.FromDateTime(DateTime.Now);
    var target = DateOnly.Parse(targetDate);
    var days = target.DayNumber - today.DayNumber;

    var result = new
    {
        today = today.ToString("yyyy-MM-dd"),
        targetDate,
        daysUntil = days
    };

    return JsonSerializer.Serialize(result);
}

/// <summary>
/// Calculates time zone differences between Australian cities.
/// </summary>
[Description("Calculate the time difference between two Australian cities and optionally convert a specific time. Returns time zone offset and converted time if provided.")]
static string CalculateTimeZone(
    [Description("Origin Australian city (e.g., 'Sydney', 'Melbourne', 'Brisbane', 'Perth', 'Adelaide', 'Hobart', 'Darwin', 'Canberra')")] string fromCity,
    [Description("Destination Australian city (e.g., 'Sydney', 'Melbourne', 'Brisbane', 'Perth', 'Adelaide', 'Hobart', 'Darwin', 'Canberra')")] string toCity,
    [Description("Optional: time in origin city in 24-hour format HH:mm (e.g., '14:30')")] string? localTime = null)
{
    try
    {
        var fromTz = GetTimeZone(fromCity);
        var toTz = GetTimeZone(toCity);

        var nowUtc = DateTime.UtcNow;
        var fromOffset = fromTz.GetUtcOffset(nowUtc);
        var toOffset = toTz.GetUtcOffset(nowUtc);
        var diffHours = (toOffset - fromOffset).TotalHours;

        if (string.IsNullOrWhiteSpace(localTime))
        {
            return JsonSerializer.Serialize(new
            {
                fromCity,
                toCity,
                timeDifferenceHours = diffHours,
                description = diffHours switch
                {
                    > 0 => $"{toCity} is {diffHours} hours ahead of {fromCity}",
                    < 0 => $"{toCity} is {Math.Abs(diffHours)} hours behind {fromCity}",
                    _ => "Same time zone"
                }
            });
        }

        if (!TimeOnly.TryParse(localTime, out var time))
        {
            return JsonSerializer.Serialize(new { error = "Invalid time format (HH:mm)" });
        }

        var today = DateTime.Today;
        var fromLocal = new DateTime(
            today.Year, today.Month, today.Day,
            time.Hour, time.Minute, 0,
            DateTimeKind.Unspecified);

        var utc = TimeZoneInfo.ConvertTimeToUtc(fromLocal, fromTz);
        var toLocal = TimeZoneInfo.ConvertTimeFromUtc(utc, toTz);

        return JsonSerializer.Serialize(new
        {
            fromCity,
            toCity,
            fromTime = localTime,
            toTime = toLocal.ToString("HH:mm"),
            dayAdjustment = toLocal.Date.CompareTo(fromLocal.Date) switch
            {
                0 => "same day",
                > 0 => "next day",
                _ => "previous day"
            }
        });
    }
    catch (Exception ex)
    {
        return JsonSerializer.Serialize(new { error = ex.Message });
    }
}

/// <summary>
/// Helper method to get TimeZoneInfo for Australian cities.
/// </summary>
static TimeZoneInfo GetTimeZone(string city)
{
    var cityTimeZones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Sydney", "Australia/Sydney" },
        { "Melbourne", "Australia/Melbourne" },
        { "Brisbane", "Australia/Brisbane" },
        { "Perth", "Australia/Perth" },
        { "Adelaide", "Australia/Adelaide" },
        { "Hobart", "Australia/Hobart" },
        { "Darwin", "Australia/Darwin" },
        { "Canberra", "Australia/Sydney" },      // Canberra uses Sydney timezone
        { "Gold Coast", "Australia/Brisbane" },  // Gold Coast uses Brisbane timezone
        { "Newcastle", "Australia/Sydney" },     // Newcastle uses Sydney timezone
        { "Cairns", "Australia/Brisbane" }       // Cairns uses Brisbane timezone
    };

    if (!cityTimeZones.TryGetValue(city, out var tzId))
    {
        throw new ArgumentException($"Unknown city: {city}");
    }

    return TimeZoneInfo.FindSystemTimeZoneById(tzId);
}


// ==================== Helper Methods ====================

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