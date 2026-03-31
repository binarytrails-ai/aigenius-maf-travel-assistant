using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ContosoTravelAgent.Host.Agents.Workflow;

public class ContosoTravelWorkflowAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    public ContosoTravelWorkflowAgentFactory(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
    }

    public async Task<AIAgent> CreateAsync()
    {
        var triageAgentFactory = _serviceProvider.GetRequiredService<TriageAgentFactory>();
        var tripAdvisorAgentFactory = _serviceProvider.GetRequiredService<TripAdvisorAgentFactory>();
        var flightSearchAgentFactory = _serviceProvider.GetRequiredService<FlightBookingAgentFactory>();

        var triageAgent = await triageAgentFactory.CreateAsync();
        var tripAdvisorAgent = await tripAdvisorAgentFactory.CreateAsync();
        var flightSearchAgent = await flightSearchAgentFactory.CreateAsync();

        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
            .WithHandoffs(triageAgent, [tripAdvisorAgent, flightSearchAgent])
            .WithHandoffs(tripAdvisorAgent, [flightSearchAgent, triageAgent])
            .WithHandoffs(flightSearchAgent, [tripAdvisorAgent, triageAgent])
            .Build();

        // The workflow is already an AIAgent type, can be used directly
        AIAgent workflowAgent = workflow.AsAIAgent();

        // Apply OpenTelemetry and logging
        var logger = _loggerFactory.CreateLogger<ContosoTravelWorkflowAgentFactory>();
        workflowAgent = workflowAgent.AsBuilder()
            .UseOpenTelemetry(Constants.ApplicationId, options =>
            {
                options.EnableSensitiveData = true;
            })
            .UseLogging(_loggerFactory)
            .Build();

        return workflowAgent;
    }
}
