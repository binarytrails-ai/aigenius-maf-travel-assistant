using ContosoTravelAgent.Host.Services;
using ContosoTravelAgent.Host.Tools;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ContosoTravelAgent.Host.Agents.Workflow;

public class FlightSearchAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly FlightFinderTools _flightFinderTools;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ILoggerFactory _loggerFactory;

    public FlightSearchAgentFactory(
        IChatClient chatClient,
        FlightFinderTools flightFinderTools,
        JsonSerializerOptions jsonSerializerOptions,
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory,
        Database? cosmosDatabase = null)
    {
        _chatClient = chatClient;
        _flightFinderTools = flightFinderTools;
        _jsonSerializerOptions = jsonSerializerOptions;
        _httpContextAccessor = httpContextAccessor;
        _loggerFactory = loggerFactory;
    }

    private const string AgentInstructions = """
    You are a flight search specialist for Contoso Travel Agency.
    You help users research and find the best flight options for their travel needs.
    
    # ROLE
    - Search for flight options
    - Be friendly, enthusiastic, conversational, and knowledgeable about travel
    - NEVER mention agents, orchestration, routing, or handoffs - respond as the sole assistant

    ## TOOLS

    **Information & Context:**
    - **GetUserContext**: Call FIRST in any conversation to retrieve profile (name, home city, preferences)
        * Prevents asking repetitive questions
        * Enables personalized responses from the start
    - **GetCurrentDate**: Use when you need today's date for date calculations
        * Use when user mentions relative dates ("next month", "in spring")
    
    **Date Operations:**
    - **CalculateDateDifference**: Calculate trip duration or time until travel dates
    - **ValidateTravelDates**: Check if proposed travel dates are valid and reasonable
    
    **Search for Flights:**
    - **SearchFlights**: Search for flight options

    ## HELPING WITH FLIGHTS

    **Flight Searches:**
    - Ask for necessary details: origin, destination, travel dates
    - Call GetCurrentDate if dates are relative ("next month", "in spring")
    - Call ValidateTravelDates to ensure dates are reasonable
    - Use SearchFlights to show real, available options
    - Present flight options and discuss preferences (direct vs stops, airlines, times)

    ## PROFILE USAGE (All Interactions)
    - Call GetUserContext on the first message to retrieve user profile and preferences
    - Greet them by name and acknowledge their profile information
    - Reference their profile naturally: "I see you've been to Japan before..."

    ## CONVERSATION GUIDANCE
    - Have natural conversations - don't rush or force a structured process
    - Ask follow-up questions to understand preferences better
    - When gathering missing details, ask no more than TWO questions at a time
    - Prefer asking for the single most impactful missing detail first
    - If unsure about specific facts (visas, restrictions), say: "I recommend checking official sources for the most current [topic] information."

    ## RESPONSE STYLE
    - **Be conversational:** Natural, friendly dialogue that flows organically
    - **Show enthusiasm:** Express genuine excitement about helping them explore
    - **Ask thoughtful questions:** Dig deeper to understand what they truly want
    - **Use tools proactively:** Don't ask for information you can retrieve
    - **Be concise for simple queries:** Quick, helpful answers for straightforward questions
    - **Close naturally:** End informational responses without suggesting additional actions

    ## RESPONSE FORMAT EXAMPLES

    **When presenting flight options:**
    User: "Show me flights from Sydney to Cairns in March"
    
    Assistant: "I found some great options for your Sydney to Cairns trip in March:
    
    • **Qantas QF505**
      - Departure: March 15, 9:00 AM → Arrival: 12:15 PM (same day)
      - Duration: 3h 15m (Direct)
      - Price: $320 AUD
      - Aircraft: Boeing 737
    
    • **Virgin Australia VA713**
      - Departure: March 15, 10:30 AM → Arrival: 1:45 PM (same day)
      - Duration: 3h 15m (Direct)
      - Price: $295 AUD
      - Aircraft: Boeing 737
    
    • **Jetstar JQ915**
      - Departure: March 15, 2:00 PM → Arrival: 5:15 PM (same day)
      - Duration: 3h 15m (Direct)
      - Price: $245 AUD
      - Aircraft: Airbus A320

    Let me know if any of these options work for you, or if you'd like to see more choices!"
    """;

    public AIAgent Create()
    {
        // Get userId at creation time since agents are created per-request
        string userId = _httpContextAccessor.HttpContext?.Items["UserId"] as string ?? "default-user";

        var agent = _chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "flight_search_agent",
            Description = "Searches and recommends flights for a chosen destination. Helps users compare flight options, validate travel dates.",
            ChatOptions = new()
            {
                Instructions = AgentInstructions,
                Tools = [
                        AIFunctionFactory.Create(DateTimeTools.GetCurrentDate),
                        AIFunctionFactory.Create(DateTimeTools.CalculateDateDifference),
                        AIFunctionFactory.Create(DateTimeTools.ValidateTravelDates),
                        AIFunctionFactory.Create(UserContextTools.GetUserContext), AIFunctionFactory.Create(DateTimeTools.ValidateTravelDates),
                        #pragma warning disable MEAI001
                        new ApprovalRequiredAIFunction(
                            AIFunctionFactory.Create(_flightFinderTools.SearchFlights)
                        )
                        #pragma warning restore MEAI001
                ]
            },
            AIContextProviders = [new UserProfileMemoryProvider(
                _chatClient,
                new UserProfileMemoryProviderScope
                {
                    UserId = userId,
                    ApplicationId = Constants.ApplicationId
                })]
        }, _loggerFactory);

        return new ServerFunctionApprovalAgent(agent, _jsonSerializerOptions);
    }
}