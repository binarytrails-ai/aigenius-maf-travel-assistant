#pragma warning disable MAAI001 // FileAgentSkillsProvider is experimental

using ContosoTravelAgent.Host.Models;
using ContosoTravelAgent.Host.Services;
using ContosoTravelAgent.Host.Tools;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Embeddings;
using System.Text.Json;

namespace ContosoTravelAgent.Host.Agents;

public class ContosoTravelAgentBuilder
{
    private readonly IChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly Database? _cosmosDatabase;
    private readonly ILoggerFactory _loggerFactory;
    private readonly McpClient _mcpClient;
    private readonly ContosoTravelAppConfig _config;

    /// <summary>
    /// Initializes a new instance of the ContosoTravelAgentFactory class.
    /// </summary>
    /// <param name="chatClient">Chat client for LLM interactions.</param>
    /// <param name="embeddingClient">Embedding client for vector operations.</param>
    /// <param name="httpContextAccessor">HTTP context accessor.</param>
    /// <param name="jsonSerializerOptions">JSON serialization options.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="sp">Service provider for resolving dependencies.</param>
    /// <param name="config">Application configuration.</param>
    /// <param name="cosmosDatabase">Cosmos DB database instance (optional).</param>
    public ContosoTravelAgentBuilder(
        IChatClient chatClient,
        EmbeddingClient embeddingClient,
        IHttpContextAccessor httpContextAccessor,
        JsonSerializerOptions jsonSerializerOptions,
        ILoggerFactory loggerFactory,
        IServiceProvider sp,
        ContosoTravelAppConfig config,
        Database? cosmosDatabase = null)
    {
        _chatClient = chatClient;
        _embeddingClient = embeddingClient;
        _httpContextAccessor = httpContextAccessor;
        _jsonSerializerOptions = jsonSerializerOptions;
        _cosmosDatabase = cosmosDatabase;
        _loggerFactory = loggerFactory;
        _mcpClient = sp.GetRequiredKeyedService<McpClient>("mcp-contoso-travel");
        _config = config;
    }

    private const string AgentInstructions = """
    You are Contoso Travel Assistant, an intelligent assistant for Contoso Travel Agency.
    Introduce yourself as "Contoso Travel Assistant" and help travelers with all their travel needs!

    # ROLE
    - Help travelers discover destinations and plan trips
    - Provide travel advice on destinations, visas, timing, and costs
    - Load relevant skills when a request aligns with an available skill's domain - skills provide specialized guidance for handling specific travel tasks
    - Be friendly, enthusiastic, conversational, and knowledgeable about travel

    ## CONVERSATION STYLE
    - Have natural, flowing conversations - don't interrogate or rush through questions
    - Ask follow-up questions to understand preferences better (no more than TWO at a time)
    - Show genuine enthusiasm about helping travelers explore
    - Be concise for simple queries, detailed when planning requires it
    - Paint vivid pictures of destinations to inspire travelers

    ## RESPONSE GUIDELINES
    - Provide practical, actionable travel advice
    - Explain pros/cons and trade-offs when presenting options
    - Include timing, budget, and logistics considerations
    - Close naturally without forcing next steps on informational queries
    """;

    public async Task<AIAgent> CreateAsync()
    {
        // Get userId from HttpContext (fallback to threadId or default-user)
        string userId = _httpContextAccessor.HttpContext?.Items["UserId"] as string
            ?? _httpContextAccessor.HttpContext?.Items["ThreadId"] as string
            ?? "default-user";

        var tools = await GetTools();

        var logger = _loggerFactory.CreateLogger<ContosoTravelAgentBuilder>();

        var skillPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "skills/flight-booking"),
            Path.Combine(AppContext.BaseDirectory, "skills/trip-planner"),
            Path.Combine(AppContext.BaseDirectory, "skills/visa-assistance")
        };

        var skillsProvider = new AgentSkillsProvider(skillPaths: skillPaths, loggerFactory: _loggerFactory);
        var userProfileMemoryProvider = GetUserProfileMemoryProvider(userId);
        var contextProviders = new List<AIContextProvider> { skillsProvider, userProfileMemoryProvider };

        AIAgent agent = _chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = Constants.AgentName,
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.Text,
                Instructions = AgentInstructions,
                Tools = [
                        AIFunctionFactory.Create(UserContextTools.GetUserContext),
                        AIFunctionFactory.Create(DateTimeTools.GetCurrentDate),
                        AIFunctionFactory.Create(DateTimeTools.CalculateDateDifference),
                        AIFunctionFactory.Create(DateTimeTools.ValidateTravelDates),
                        AIFunctionFactory.Create(DateTimeTools.ValidateTravelDates),
                        .. tools]
            },
            AIContextProviders = contextProviders,
        });

        agent = agent.AsBuilder().UseOpenTelemetry(Constants.ApplicationId, options =>
        {
            options.EnableSensitiveData = true;
        }).UseLogging(_loggerFactory).Build();

        return new ServerFunctionApprovalAgent(agent, _jsonSerializerOptions);
    }

    private async Task<List<AITool>> GetTools()
    {
        var mcpTools = await _mcpClient.ListToolsAsync();
        var processedTools = new List<AITool>();
        foreach (var tool in mcpTools)
        {
            var toolName = GetToolName(tool);
            if (string.Equals(toolName, "book_flight", StringComparison.OrdinalIgnoreCase))
            {
                // Wrap BookFlight with ApprovalRequiredAIFunction
                AIFunction bookFlightWithApproval = new ApprovalRequiredAIFunction(tool);
                processedTools.Add(bookFlightWithApproval);
            }
            else
            {
                processedTools.Add(tool);
            }
        }

        return processedTools;
    }

    private string GetToolName(AITool tool)
    {
        // Use ToString to get the tool name
        var name = tool.ToString();
        return name ?? "Unknown";
    }

    private UserProfileMemoryProvider GetUserProfileMemoryProvider(string userId)
    {
        return new UserProfileMemoryProvider(
            _chatClient,
            _cosmosDatabase!,
            _config.CosmosDbUserProfileContainer ?? "UserProfiles",
            new UserProfileMemoryProviderScope
            {
                UserId = userId,
                ApplicationId = Constants.ApplicationId
            },
            loggerFactory: _loggerFactory);
    }
}