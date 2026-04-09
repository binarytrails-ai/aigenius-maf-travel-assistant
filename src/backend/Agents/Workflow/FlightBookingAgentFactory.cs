#pragma warning disable MAAI001 // FileAgentSkillsProvider is experimental

using ContosoTravelAgent.Host.Models;
using ContosoTravelAgent.Host.Services;
using ContosoTravelAgent.Host.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text.Json;

namespace ContosoTravelAgent.Host.Agents.Workflow;

public class FlightBookingAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly McpClient _mcpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Database? _cosmosDatabase;
    private readonly ContosoTravelAppConfig _config;

    public FlightBookingAgentFactory(
        IChatClient chatClient,
        McpClient mcpClient,
        JsonSerializerOptions jsonSerializerOptions,
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory,
        ContosoTravelAppConfig config,
        Database? cosmosDatabase = null)
    {
        _chatClient = chatClient;
        _mcpClient = mcpClient;
        _jsonSerializerOptions = jsonSerializerOptions;
        _httpContextAccessor = httpContextAccessor;
        _loggerFactory = loggerFactory;
        _cosmosDatabase = cosmosDatabase;
        _config = config;
    }

    private const string AgentInstructions = """
    You are the Flight Booking specialist for Contoso Travel Agency.
    Help users search, compare, and book flights.

    # YOUR RESPONSIBILITIES
    - Search for flight options and present results clearly
    - Handle ALL flight-related follow-ups including confirmations and bookings
    - Book flights when user selects or approves an option
    - Be friendly, enthusiastic, conversational, and knowledgeable about travel

    ## CONVERSATION STYLE
    - Have natural, flowing conversations
    - Ask follow-up questions to understand preferences (no more than TWO at a time)
    - Be concise for simple queries, detailed when presenting options

    ## TOOL USAGE
    - **GetUserContext**: Call FIRST to retrieve profile and enable personalized responses
    - **GetCurrentDate**: Use for relative dates ("next month", "in spring")
    - **SearchFlights**: Search for available flight options
    - **BookFlight**: Book a flight after user approval
    - **CalculateDateDifference/ValidateTravelDates**: For date calculations and validation

    ## FLIGHT SEARCHES
    - Gather necessary details: origin, destination, travel dates
    - Present options with price, duration, airlines, and times
    - Discuss preferences (direct vs stops, timing, airlines)
    """;
    public async Task<AIAgent> CreateAsync()
    {
        // Get userId at creation time since agents are created per-request
        string userId = _httpContextAccessor.HttpContext?.Items["UserId"] as string ?? "default-user";

        // Get MCP tools for flight operations
        var mcpTools = await GetMcpToolsAsync();

        // Set up skills provider for flight-booking skill
        var skillPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "skills/flight-booking")
        };
        var skillsProvider = new AgentSkillsProvider(skillPaths: skillPaths, loggerFactory: _loggerFactory);

        // Set up user profile memory provider
        var userProfileMemoryProvider = new UserProfileMemoryProvider(
            _chatClient,
            _cosmosDatabase!,
            _config.CosmosDbUserProfileContainer ?? "UserProfiles",
            new UserProfileMemoryProviderScope
            {
                UserId = userId,
                ApplicationId = Constants.ApplicationId
            },
            loggerFactory: _loggerFactory);

        AIAgent agent = _chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "flight_booking_agent",
            Description = "Searches and books flights for travel. Helps users find flight options, compare prices, validate travel dates, and complete flight bookings.",
            ChatOptions = new()
            {
                Instructions = AgentInstructions,
                Tools = [
                    AIFunctionFactory.Create(DateTimeTools.GetCurrentDate),
                    AIFunctionFactory.Create(DateTimeTools.CalculateDateDifference),
                    AIFunctionFactory.Create(DateTimeTools.ValidateTravelDates),
                    AIFunctionFactory.Create(UserContextTools.GetUserContext),
                    .. mcpTools
                ]
            },
            AIContextProviders = [skillsProvider, userProfileMemoryProvider]
        }, _loggerFactory);

        // Apply OpenTelemetry and logging
        var logger = _loggerFactory.CreateLogger<FlightBookingAgentFactory>();
        agent = agent.AsBuilder()
            .UseOpenTelemetry(Constants.ApplicationId, options =>
            {
                options.EnableSensitiveData = true;
            })
            .UseLogging(_loggerFactory)
            .Use(FunctionCallMiddleware)
            .Build();

        return new ServerFunctionApprovalAgent(agent, _jsonSerializerOptions);
    }

    private async Task<List<AITool>> GetMcpToolsAsync()
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

    private static string GetToolName(AITool tool)
    {
        var name = tool.ToString();
        return name ?? "Unknown";
    }

    async ValueTask<object?> FunctionCallMiddleware(AIAgent agent, FunctionInvocationContext context, Func<FunctionInvocationContext,
        CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Function Name: {context!.Function.Name} - Middleware 1 Pre-Invoke");
        var result = await next(context, cancellationToken);
        Console.WriteLine($"Function Name: {context!.Function.Name} - Middleware 1 Post-Invoke");

        return result;
    }
}