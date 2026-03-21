using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ContosoTravelAgent.Host.Agents.Workflow;

public class ContosoTravelWorkflowAgentFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ContosoTravelWorkflowAgentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public AIAgent Create()
    {
        var triageAgentFactory = _serviceProvider.GetRequiredService<TriageAgentFactory>();
        var tripAdvisorAgentFactory = _serviceProvider.GetRequiredService<TripAdvisorAgentFactory>();
        var flightSearchAgentFactory = _serviceProvider.GetRequiredService<FlightSearchAgentFactory>();

        var triageAgent = triageAgentFactory.Create();
        var tripAdvisorAgent = tripAdvisorAgentFactory.Create();
        var flightSearchAgent = flightSearchAgentFactory.Create();

        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
            //// Triage to specialists - based on routing guidelines
            .WithHandoff(triageAgent, tripAdvisorAgent,
                "User asks general travel questions (costs, best time to visit, what to see) OR asks questions about existing trips OR wants to plan a new trip.")
            .WithHandoff(triageAgent, flightSearchAgent,
                "User wants to search for flights, find flights, look for flights, book flights, or asks about flight options, prices, schedules, or travel dates. Includes requests like 'find flights from X to Y', 'show me flights', 'search flights'.")
            .Build();

        // Convert the workflow to an AIAgent and return it
        return workflow.AsAgent(id: "travel-workflow-agent", name: "ContosoTravelWorkflowAgent");
    }
}
