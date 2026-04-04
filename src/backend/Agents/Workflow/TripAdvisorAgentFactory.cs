#pragma warning disable MAAI001 // FileAgentSkillsProvider is experimental

using ContosoTravelAgent.Host.Models;
using ContosoTravelAgent.Host.Services;
using ContosoTravelAgent.Host.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace ContosoTravelAgent.Host.Agents.Workflow;

public class TripAdvisorAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly Database? _cosmosDatabase;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ContosoTravelAppConfig _config;

    public TripAdvisorAgentFactory(
        IChatClient chatClient,
        IHttpContextAccessor httpContextAccessor,
        JsonSerializerOptions jsonSerializerOptions,
        ILoggerFactory loggerFactory,
        ContosoTravelAppConfig config,
        Database? cosmosDatabase = null)
    {
        _chatClient = chatClient;
        _httpContextAccessor = httpContextAccessor;
        _jsonSerializerOptions = jsonSerializerOptions;
        _cosmosDatabase = cosmosDatabase;
        _loggerFactory = loggerFactory;
        _config = config;
    }

    private const string AgentInstructions = """
    You are the Trip Planning specialist for Contoso Travel Agency.
    Help travelers discover destinations and provide personalized travel recommendations.

    # YOUR RESPONSIBILITIES
    - Provide personalized destination recommendations based on preferences
    - Offer travel advice on destinations, timing, activities, and costs
    - Answer visa and travel requirement questions
    - Be friendly, enthusiastic, conversational, and knowledgeable about travel

    ## CONVERSATION STYLE
    - Have natural, flowing conversations - don't interrogate or rush
    - Ask follow-up questions to understand preferences (no more than TWO at a time)
    - Show genuine enthusiasm about helping travelers explore
    - Be concise for simple queries, detailed when planning requires it

    ## TOOL USAGE
    - **GetUserContext**: Call FIRST to retrieve profile and enable personalized responses
    - **GetCurrentDate**: Use for relative dates ("next month", "in spring")
    - **CalculateDateDifference/ValidateTravelDates**: For date calculations and validation

    ## DESTINATION RECOMMENDATIONS
    - Require at least TWO preferences before suggesting destinations (budget, travel style, interests)
    - ALL recommendations MUST be within Australia and New Zealand
    - Present options with timing, budget, and activity considerations
    - Reference user profile naturally when available
    """;

    public Task<AIAgent> CreateAsync()
    {
        // Get userId at creation time since agents are created per-request
        string userId = _httpContextAccessor.HttpContext?.Items["UserId"] as string ?? "default-user";

        // Set up skills provider for trip-planner and visa-assistance skills
        var skillPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "skills/trip-planner"),
            Path.Combine(AppContext.BaseDirectory, "skills/visa-assistance")
        };
        var skillsProvider = new AgentSkillsProvider(skillPaths: skillPaths, loggerFactory: _loggerFactory);

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

        var contextProviders = new List<AIContextProvider>
        {
            skillsProvider,
            userProfileMemoryProvider
        };

        AIAgent agent = _chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "trip_advisor_agent",
            Description = "Provides personalized destination recommendations, travel advice, and visa information for Contoso Travel Agency.",
            ChatOptions = new()
            {
                Instructions = AgentInstructions,
                Tools = [
                    AIFunctionFactory.Create(DateTimeTools.GetCurrentDate),
                    AIFunctionFactory.Create(DateTimeTools.CalculateDateDifference),
                    AIFunctionFactory.Create(DateTimeTools.ValidateTravelDates),
                    AIFunctionFactory.Create(UserContextTools.GetUserContext)
                ]
            },
            AIContextProviders = contextProviders
        }, _loggerFactory);

        // Apply OpenTelemetry and logging
        agent = agent.AsBuilder()
            .UseOpenTelemetry(Constants.ApplicationId, options =>
            {
                options.EnableSensitiveData = true;
            })
            .UseLogging(_loggerFactory)
            .Build();

        return Task.FromResult(agent);
    }
}
