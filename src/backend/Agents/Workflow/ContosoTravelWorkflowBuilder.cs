using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ContosoTravelAgent.Host.Agents.Workflow;

public class ContosoTravelWorkflowAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ContosoTravelWorkflowAgentFactory(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task<AIAgent> CreateAsync()
    {
        var triageAgentFactory = _serviceProvider.GetRequiredService<TriageAgentFactory>();
        var tripAdvisorAgentFactory = _serviceProvider.GetRequiredService<TripAdvisorAgentFactory>();
        var flightSearchAgentFactory = _serviceProvider.GetRequiredService<FlightBookingAgentFactory>();

        var triageAgent = triageAgentFactory.Create();
        var tripAdvisorAgent = tripAdvisorAgentFactory.Create();
        var flightSearchAgent = await flightSearchAgentFactory.CreateAsync();

        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
            // Initial routing from triage to specialists
            .WithHandoff(triageAgent, tripAdvisorAgent,
                "User asks about destinations, trip planning, visa requirements, or general travel advice.")
            .WithHandoff(triageAgent, flightSearchAgent,
                "User asks about flights, booking flights, approves a flight booking, or any flight-related continuation including approval responses.")
            // Bidirectional handoffs between specialists for topic changes
            .WithHandoff(tripAdvisorAgent, flightSearchAgent,
                "User wants to search for flights or book a flight now.")
            .WithHandoff(flightSearchAgent, tripAdvisorAgent,
                "User asks about destinations, places to visit, or travel advice.")
            // Return to triage only when explicitly asked or conversation complete
            .WithHandoff(tripAdvisorAgent, triageAgent,
                "User explicitly asks for a different type of help or says they're done with trip planning.")
            .WithHandoff(flightSearchAgent, triageAgent,
                "User explicitly asks for a different type of help or says they're done with flight booking.")
            .Build();

        // The workflow is already an AIAgent type, can be used directly
        AIAgent workflowAgent = workflow.AsAIAgent();

        // Apply OpenTelemetry and logging
        workflowAgent = workflowAgent.AsBuilder()
            .UseOpenTelemetry(Constants.ApplicationId, options =>
            {
                options.EnableSensitiveData = true;
            })
            .UseLogging(_loggerFactory)
            .Build();

        return new ServerFunctionApprovalAgent(workflowAgent, _jsonSerializerOptions);
    }
}
