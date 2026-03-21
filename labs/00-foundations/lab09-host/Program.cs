using Azure.AI.OpenAI;
using DotNetEnv;
using Lab05Host.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ClientModel;

const string SourceName = "TravelAssistant";
const string ServiceName = "TravelAssistant";

// Configure JSON serialization for Azure SDK compatibility
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

// Load environment variables
LoadEnvironmentVariables();

// Create ASP.NET Core web application
var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName, serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource(SourceName)
        .AddSource("Microsoft.Agents.AI")
        .AddSource("Microsoft.Extensions.AI")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithLogging(logging => logging
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

// Configure console logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Configure CORS (development only - restrict in production)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Add AGUI services for agent UI
builder.Services.AddAGUI();

// Create chat client
var chatClient = CreateChatClient();
if (chatClient == null)
{
    Console.WriteLine("[ERROR] Failed to create chat client. Exiting...");
    return;
}

// Create tools for the agent
var tools = new List<AITool>
{
    AIFunctionFactory.Create(DateTimeTools.GetCurrentDate),
    AIFunctionFactory.Create(DateTimeTools.CalculateDaysUntil),
    AIFunctionFactory.Create(FlightSearchTools.SearchFlights)
};

Console.WriteLine($"[INFO] Created {tools.Count} tools for the agent\n");

// Build the application first to get services
var app = builder.Build();

// Get logger factory from the built application
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Create the travel agent
AIAgent travelAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistant",
    ChatOptions = new()
    {
        Instructions = """
            You are a helpful travel planning assistant with date calculation tools.
            
            Use the tools to answer questions about dates and durations.
            Provide friendly, conversational responses based on the tool results.
            """,
        Tools = tools
    }
});

travelAgent = travelAgent.AsBuilder()
    .UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
    .UseLogging(loggerFactory)
    .Build();

Console.WriteLine("[INFO] Travel Assistant Agent created successfully\n");

// Configure middleware
app.UseCors();

// Map OpenAI-compatible completions endpoint
app.MapOpenAIChatCompletions(travelAgent);
logger.LogInformation("Mapped OpenAI completions endpoint at: /v1/chat/completions");

// Map AGUI endpoint for interactive development UI
app.MapAGUI("/agent/travel", travelAgent);
logger.LogInformation("Mapped AGUI endpoint at: /agent/travel");

// Display startup information
logger.LogInformation("HOST STARTED - Agent is now available at the following endpoints:");
logger.LogInformation("  Agent UI:     http://localhost:5000/agent/travel");
logger.LogInformation("  OpenAI API:   http://localhost:5000/TravelAssistant/v1/chat/completions");
logger.LogInformation("Press Ctrl+C to shut down");

// Run the web server
await app.RunAsync();


// ==================== Helper Methods ====================

static void LoadEnvironmentVariables()
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env");
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
        Console.WriteLine($"[INFO] Loaded environment from: {envPath}\n");
    }
    else
    {
        Console.WriteLine($"[WARN] .env file not found at: {envPath}");
        Console.WriteLine("[WARN] Using system environment variables\n");
    }
}

static IChatClient? CreateChatClient()
{
    var useGitHub = Environment.GetEnvironmentVariable("USE_GITHUB_MODELS")?.ToLower() == "true";
    
    try
    {
        if (useGitHub)
        {
            Console.WriteLine("[INFO] Using GitHub Models\n");
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var modelId = Environment.GetEnvironmentVariable("GITHUB_TEXT_MODEL_ID") ?? "gpt-4o";
            var baseUrl = Environment.GetEnvironmentVariable("GITHUB_MODELS_BASE_URL") 
                ?? "https://models.inference.ai.azure.com";

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("[ERROR] GITHUB_TOKEN environment variable not set");
                return null;
            }

            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
            var openAiClient = new OpenAIClient(new ApiKeyCredential(token), clientOptions);
            
            return openAiClient.GetChatClient(modelId)
                .AsIChatClient()
                .AsBuilder()
                .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
                .Build();
        }
        else
        {
            Console.WriteLine("[INFO] Using Azure OpenAI\n");
            var endpoint = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_ENDPOINT");
            var key = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_KEY");
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_TEXT_MODEL_NAME") ?? "gpt-4o";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
            {
                Console.WriteLine("[ERROR] Azure AI Services credentials not configured");
                Console.WriteLine("[ERROR] Set AZURE_AI_SERVICES_ENDPOINT and AZURE_AI_SERVICES_KEY");
                return null;
            }

            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
            return azureClient.GetChatClient(deploymentName)
                .AsIChatClient()
                .AsBuilder()
                .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
                .Build();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to create chat client: {ex.Message}");
        return null;
    }
}
