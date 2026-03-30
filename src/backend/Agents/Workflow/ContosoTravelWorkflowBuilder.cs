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
        var flightSearchAgentFactory = _serviceProvider.GetRequiredService<FlightSearchAgentFactory>();

        var triageAgent = triageAgentFactory.Create();
        var tripAdvisorAgent = tripAdvisorAgentFactory.Create();
        var flightSearchAgent = await flightSearchAgentFactory.CreateAsync();

        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
            // Triage to specialists - based on routing guidelines
            .WithHandoff(triageAgent, tripAdvisorAgent,
                "User asks general travel questions (costs, best time to visit, what to see) OR asks questions about existing trips OR wants to plan a new trip OR asks about visas, entry requirements, or travel documents.")
            .WithHandoff(triageAgent, flightSearchAgent,
                "User wants to search for flights, find flights, look for flights, book flights, or asks about flight options, prices, schedules, or travel dates. Includes requests like 'find flights from X to Y', 'show me flights', 'search flights'.")
            // Allow cross-agent handoffs for seamless conversation flow
            .WithHandoff(tripAdvisorAgent, flightSearchAgent,
                "User is ready to search for flights after discussing destination options. User mentions specific dates, wants to book, or says 'find flights', 'search flights', 'show me flights'.")
            .WithHandoff(flightSearchAgent, tripAdvisorAgent,
                "User wants to go back to trip planning, asks about destinations, activities, visa requirements, or needs more travel advice.")
            .Build();

        // The workflow is already an AIAgent type, can be used directly
        AIAgent workflowAgent = workflow;

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
