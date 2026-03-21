using ContosoTravel.ServiceDefaults;
using ContosoTravelAgent.Host.Models;
using Microsoft.Extensions.Options;

namespace ContosoTravelAgent.Host.Extensions;

public static class ConfigurationExtensions
{
    public static ContosoTravelAppConfig LoadContosoTravelConfig(this IServiceCollection services, IConfiguration configuration)
    {
        EnvironmentVariableHelper.LoadEnvironmentVariables();

        // Azure AI configuration
        var azureProjectEndpoint = EnvironmentVariableHelper.GetConfigValue("AZURE_AI_PROJECT_ENDPOINT", configuration);
        var azureAiFoundryServiceEndpoint = EnvironmentVariableHelper.GetConfigValue("AZURE_AI_FOUNDRY_SERVICE_ENDPOINT", configuration);
        var azureAiServicesEndpoint = EnvironmentVariableHelper.GetConfigValue("AZURE_AI_SERVICES_ENDPOINT", configuration);
        var azureAiServicesKey = EnvironmentVariableHelper.GetConfigValue("AZURE_AI_SERVICES_KEY", configuration);
        var azureProjectName = EnvironmentVariableHelper.GetConfigValue("AZURE_AI_PROJECT_NAME", configuration);
        var azureLocation = EnvironmentVariableHelper.GetConfigValue("AZURE_LOCATION", configuration);
        var azureSubscriptionId = EnvironmentVariableHelper.GetConfigValue("AZURE_SUBSCRIPTION_ID", configuration);
        var azureTenantId = EnvironmentVariableHelper.GetConfigValue("AZURE_TENANT_ID", configuration);
        var textModelName = EnvironmentVariableHelper.GetConfigValue("AZURE_TEXT_MODEL_NAME", configuration, "gpt-4o");
        var embeddingModelName = EnvironmentVariableHelper.GetConfigValue("AZURE_EMBEDDING_MODEL_NAME", configuration, "text-embedding-ada-002");
        var azureSearchEndpoint = EnvironmentVariableHelper.GetConfigValue("AZURE_SEARCH_SERVICE_ENDPOINT", configuration);
        var azureSearchAdminKey = EnvironmentVariableHelper.GetConfigValue("AZURE_AI_SEARCH_ADMIN_KEY", configuration);
        var otlpEndpoint = EnvironmentVariableHelper.GetConfigValue("OTEL_EXPORTER_OTLP_ENDPOINT", configuration);

        // Application Insights - check both keys
        var applicationInsightsConnectionString = EnvironmentVariableHelper.GetConfigValue("APPLICATIONINSIGHTS_CONNECTION_STRING", configuration)
                                                  ?? configuration["ApplicationInsights:ConnectionString"];

        // MCP tool configuration
        var mcpFlightSearchToolBaseUrl = EnvironmentVariableHelper.GetConfigValue("MCP_FLIGHT_SEARCH_TOOL_BASE_URL", configuration, "http://localhost:5002");
        var mcpFlightSearchApiKey = EnvironmentVariableHelper.GetConfigValue("MCP_FLIGHT_SEARCH_API_KEY", configuration);

        // Cosmos DB configuration
        var cosmosDbEndpoint = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_ENDPOINT", configuration);
        var cosmosDbConnectionString = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_CONNECTION_STRING", configuration);
        var cosmosDbDatabaseName = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_DATABASE_NAME", configuration);
        var cosmosDbChatHistoryContainer = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_CHAT_HISTORY_CONTAINER", configuration);
        var cosmosDbUserProfileContainer = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_USER_PROFILE_CONTAINER", configuration);
        var cosmosDbFlightsContainer = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_FLIGHTS_CONTAINER", configuration, "Flights");

        if (string.IsNullOrWhiteSpace(azureProjectEndpoint))
        {
            throw new InvalidOperationException
                ("Azure AI Project endpoint not found. Set AZURE_AI_PROJECT_ENDPOINT in .env file.");
        }

        var config = new ContosoTravelAppConfig
        {
            AzureAIProjectEndpoint = azureProjectEndpoint,
            AzureAiFoundryServiceEndpoint = azureAiFoundryServiceEndpoint,
            AzureAIServicesEndpoint = azureAiServicesEndpoint,
            AzureAIServicesKey = azureAiServicesKey,
            AzureAIProjectName = azureProjectName,
            AzureLocation = azureLocation,
            AzureSubscriptionId = azureSubscriptionId,
            AzureTenantId = azureTenantId,
            AzureTextModelName = textModelName,
            AzureEmbeddingModelName = embeddingModelName,
            AzureAISearchEndpoint = azureSearchEndpoint,
            AzureAISearchAdminKey = azureSearchAdminKey,
            OtelExporterOtlpEndpoint = otlpEndpoint,
            ApplicationInsightsConnectionString = applicationInsightsConnectionString,
            McpFlightSearchToolBaseUrl = mcpFlightSearchToolBaseUrl,
            McpFlightSearchApiKey = mcpFlightSearchApiKey,
            CosmosDbEndpoint = cosmosDbEndpoint,
            CosmosDbConnectionString = cosmosDbConnectionString,
            CosmosDbDatabaseName = cosmosDbDatabaseName,
            CosmosDbChatHistoryContainer = cosmosDbChatHistoryContainer,
            CosmosDbUserProfileContainer = cosmosDbUserProfileContainer,
            CosmosDbFlightsContainer = cosmosDbFlightsContainer
        };

        services.AddSingleton<IOptions<ContosoTravelAppConfig>>(Options.Create(config));
        services.AddSingleton(config);

        return config;
    }
}
