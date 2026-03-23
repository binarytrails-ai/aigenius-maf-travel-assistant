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

    /// <summary>
    /// Initializes a new instance of the ContosoTravelAgentFactory class.
    /// </summary>
    /// <param name="chatClient">Chat client for LLM interactions.</param>
    /// <param name="embeddingClient">Embedding client for vector operations.</param>
    /// <param name="httpContextAccessor">HTTP context accessor.</param>
    /// <param name="jsonSerializerOptions">JSON serialization options.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="cosmosDatabase">Cosmos DB database instance (optional).</param>
    public ContosoTravelAgentBuilder(
        IChatClient chatClient,
        EmbeddingClient embeddingClient,
        IHttpContextAccessor httpContextAccessor,
        JsonSerializerOptions jsonSerializerOptions,
        ILoggerFactory loggerFactory,
       IServiceProvider sp,
        Database? cosmosDatabase = null)
    {
        _chatClient = chatClient;
        _embeddingClient = embeddingClient;
        _httpContextAccessor = httpContextAccessor;
        _jsonSerializerOptions = jsonSerializerOptions;
        _cosmosDatabase = cosmosDatabase;
        _loggerFactory = loggerFactory;
        _mcpClient = sp.GetRequiredKeyedService<McpClient>("mcp-contoso-travel");
    }

    private const string AgentInstructions = """
    You are Contoso Travel Assistant, an intelligent assistant for Contoso Travel Agency.
    Introduce yourself as "Contoso Travel Assistant" and help travelers with all their travel needs!

    # ROLE
    - Help travelers discover destinations
    - Offer travel advice on destinations, timing, activities, and costs
    - Be friendly, enthusiastic, conversational, and knowledgeable about travel

    ## TOOLS

    ## HELPING WITH TRAVEL

    **Destination Recommendations:**
    - **Require at least TWO preferences before suggesting destinations:**
        * Budget range (e.g., "2000-3000 AUD", "budget-friendly", "luxury")
        * Travel style (e.g., "adventure", "relaxation", "family", "romantic", "cultural")
        * Interests/activities (e.g., "hiking", "beaches", "history", "food", "wildlife")
    - ALL destination recommendations MUST be within Australia and New Zealand
    - Suggest destinations across different Australian states and territories
    - Include cities, regions, natural attractions, and coastal areas within Australia and New Zealand
    - Don't interrogate - have a natural conversation
    - Have a conversation about options, pros/cons, best times to visit
    - **Paint vivid pictures:** "The Great Barrier Reef offers vibrant coral reefs and tropical islands..."
    - **Provide context:** "May is perfect - warm weather, fewer crowds, lower prices"

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
    - **Be concise for simple queries:** Quick, helpful answers for straightforward questions
    - **Close naturally:** End informational responses without suggesting additional actions

    ## RESPONSE FORMAT EXAMPLES

    User: "I want to plan a trip"
    Assistant: "I'd love to help! What's your budget for this trip?"
    User: "Usually around $2,000"
    Assistant: "Great! What kind of experiences do you enjoy?"
    User: "I love hiking and coastal walks"
    Assistant: "Sounds wonderful! Based on your interests, here are some destinations that combine hiking with coastal beauty:

    • **Great Ocean Road, Victoria**
      - Dramatic coastal cliffs and rainforest trails
      - Perfect for: Scenic coastal walks, wildlife spotting
      - Best time: October-April
      - Budget: Well within $2,000

    • **Sunshine Coast, Queensland**
      - Beach trails and hinterland rainforest hikes
      - Perfect for: Relaxed coastal atmosphere, great food scene
      - Best time: Year-round (mild winters)
      - Budget: Mid-range, fits your budget

    Which of these appeals to you?"
    """;

    public async Task<AIAgent> CreateAsync()
    {
        var mcpTools = await _mcpClient.ListToolsAsync();
        
        // Process MCP tools and wrap book_flight with approval requirement
        var processedTools = new List<AITool>();
        foreach (var tool in mcpTools)
        {
            var toolName = GetToolName(tool);
            if (string.Equals(toolName, "book_flight", StringComparison.OrdinalIgnoreCase))
            {
#pragma warning disable MEAI001 // Type is for evaluation purposes only
                // Wrap BookFlight with ApprovalRequiredAIFunction
                AIFunction bookFlightWithApproval = new ApprovalRequiredAIFunction(tool);
                processedTools.Add(bookFlightWithApproval);
#pragma warning restore MEAI001
            }
            else
            {
                processedTools.Add(tool);
            }
        }
        
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
                        .. processedTools]
            },
        });

        agent = agent.AsBuilder().UseOpenTelemetry(Constants.ApplicationId, options =>
        {
            options.EnableSensitiveData = true;
        }).UseLogging(_loggerFactory).Build();

        return new ServerFunctionApprovalAgent(agent, _jsonSerializerOptions);
    }

    private string GetToolName(AITool tool)
    {
        // Use ToString to get the tool name
        var name = tool.ToString();
        return name ?? "Unknown";
    }
}