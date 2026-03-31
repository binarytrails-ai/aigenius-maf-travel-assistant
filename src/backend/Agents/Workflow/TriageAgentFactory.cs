using ContosoTravelAgent.Host.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ContosoTravelAgent.Host.Agents.Workflow;

public class TriageAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly ILoggerFactory _loggerFactory;

    private const string AgentInstructions = """
        You are the triage coordinator for Contoso Travel Agency.
        
        Your ONLY job is to route customer requests to the right specialist:
        
        - **hand off to trip_advisor_agent**: Questions about destinations, trip planning, visa requirements, places to visit, travel advice
        - **hand off to flight_booking_agent**: Questions about flights, finding flights, booking flights, flight times/prices, OR any pending flight booking approvals
        
        ## CRITICAL RULES
        - Produce NO assistant message when routing
        - Do NOT acknowledge, summarize, or explain routing decisions
        - NEVER mention agents, routing, handoffs, or system internals
        - Default to Trip Advisor Agent if uncertain
        """;

    public TriageAgentFactory(
        IChatClient chatClient,
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory)
    {
        _chatClient = chatClient;
        _loggerFactory = loggerFactory;
    }

    public Task<AIAgent> CreateAsync()
    {
        AIAgent agent = _chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "triage_agent",
            Description = "Routes travel requests to the appropriate specialist agent",
            ChatOptions = new()
            {
                Instructions = AgentInstructions,
                Tools = [
                    AIFunctionFactory.Create(UserContextTools.GetUserContext),
                    AIFunctionFactory.Create(DateTimeTools.GetCurrentDate)
                ]
            }
        });

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
