// Add NuGet package references
#:package Azure.AI.OpenAI@2.1.0
#:package Azure.Identity@1.20.0
#:package Microsoft.Agents.AI@1.0.0
#:package Microsoft.Agents.AI.Abstractions@1.0.0
#:package Microsoft.Extensions.AI@10.4.1
#:package Microsoft.Extensions.AI.OpenAI@10.4.1
#:package ModelContextProtocol@1.2.0
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
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
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
if (chatClient == null)
{
    tracerProvider.Dispose();
    return;
}

// Step 4: Connect to MCP server via HTTP
appLogger.LogInformation("Connecting to MCP Flight Search server...");
var mcpClient = await CreateMcpClientAsync(loggerFactory, appLogger);
if (mcpClient == null)
{
    tracerProvider.Dispose();
    return;
}

// Step 5: Get tools from MCP server
var tools = await GetTools(mcpClient, appLogger);

// Step 6: Create agent with MCP tools and wrap with console approval handler
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistant",
    ChatOptions = new()
    {
        Instructions = """
            You are a helpful travel planning assistant with date calculation tools.
            
            Use the tools to answer questions.
            Provide friendly, conversational responses based on the tool results.
            """,
        Tools = tools
    }
})
.AsBuilder()
.UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
.UseLogging(loggerFactory)
.Build();


appLogger.LogInformation("Agent created with {ToolCount} tools", tools.Count);

// Step 8: Run the agent with a two-turn conversation
try
{
    var session = await agent.CreateSessionAsync();
    var userInput = "Please book flight QF107 for December 25, 2026 for 2 passengers. The passenger details are: First Name: John, Last Name: Doe, Passport Number: AB1234567.";
    Console.WriteLine($"User: {userInput}");
    appLogger.LogInformation("User: {UserInput}", userInput);
    var response = await agent.RunAsync(userInput, session);

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    var approvalRequests =
     response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();

#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    while (approvalRequests.Count > 0)
    {
        // Ask the user to approve each function call request.
        List<ChatMessage> userInputResponses = approvalRequests
            .ConvertAll(functionApprovalRequest =>
            {
                Console.WriteLine($"The agent would like to invoke the following function, please reply Y to approve: Name {((FunctionCallContent)functionApprovalRequest.ToolCall).Name}");
                return new ChatMessage(ChatRole.User, [functionApprovalRequest.CreateResponse
                (Console.ReadLine()?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false)]);
            });

        // Pass the user input responses back to the agent for further processing.
        response = await agent.RunAsync(userInputResponses, session);

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        approvalRequests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    }

    appLogger.LogInformation("Agent: {AgentResponse}", response);
    appLogger.LogInformation("Agent response completed");
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

// ==================== Helper Methods ====================

string GetToolName(AITool tool)
{
    // Simple approach: use ToString which should give us the tool name
    var name = tool.ToString();
    return name ?? "Unknown";
}

async Task<List<AITool>> GetTools(McpClient mcpClient, ILogger appLogger)
{
    // List available tools from MCP server
    var allMcpTools = await mcpClient.ListToolsAsync();
    appLogger.LogInformation("Retrieved {Count} tools from MCP server", allMcpTools.Count);

    var tools = new List<AITool>();

    // Add all MCP tools, wrapping BookFlight with approval requirement
    foreach (var tool in allMcpTools)
    {
        var toolName = GetToolName(tool);

        if (string.Equals(toolName, "book_flight", StringComparison.OrdinalIgnoreCase))
        {
#pragma warning disable MEAI001 // Type is for evaluation purposes only
            // Wrap BookFlight with ApprovalRequiredAIFunction
            AIFunction bookFlightWithApproval = new ApprovalRequiredAIFunction(tool);
            tools.Add(bookFlightWithApproval);
            appLogger.LogInformation("Added MCP tool with approval wrapper: {ToolName}", toolName);
#pragma warning restore MEAI001
        }
        else
        {
            tools.Add(tool);
            appLogger.LogInformation("Added MCP tool: {ToolName}", toolName);
        }
    }

    return tools;
}

async Task<McpClient?> CreateMcpClientAsync(ILoggerFactory loggerFactory, ILogger appLogger)
{
    try
    {
        // Get MCP server base URL from environment or use default
        var mcpBaseUrl = Environment.GetEnvironmentVariable("MCP_FLIGHT_SEARCH_TOOL_BASE_URL")
                         ?? "http://localhost:5002";

        // Get API key from environment or use default dev key
        var mcpApiKey = Environment.GetEnvironmentVariable("MCP_FLIGHT_SEARCH_API_KEY");

        appLogger.LogInformation("Connecting to MCP server at {BaseUrl}", mcpBaseUrl);

        // Create HTTP client for MCP transport with API key authentication
        var httpClient = new HttpClient { BaseAddress = new Uri(mcpBaseUrl) };
        httpClient.DefaultRequestHeaders.Add("X-API-KEY", mcpApiKey);

        // Configure HTTP transport
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{mcpBaseUrl}/mcp")
        };

        var transport = new HttpClientTransport(transportOptions, httpClient, loggerFactory);

        // Configure MCP client
        var clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation
            {
                Name = "Flight Search Tools MCP Client",
                Version = "1.0.0"
            }

        };

        // Create MCP client
        var client = await McpClient.CreateAsync(transport, clientOptions, loggerFactory);
        appLogger.LogInformation("Successfully connected to MCP server");

        return client;
    }
    catch (Exception ex)
    {
        appLogger.LogError(ex, "Failed to create MCP client. Make sure the MCP server is running at http://localhost:5002");
        return null;
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
