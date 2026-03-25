namespace ContosoTravelAgent.Host.Models;

public record ContosoTravelAppConfig
{
    // Azure AI configuration
    public string? AzureAIProjectEndpoint { get; init; }
    public string AzureAiFoundryServiceEndpoint { get; init; }
    public string? AzureAIServicesEndpoint { get; init; }
    public string? AzureAIServicesKey { get; init; }
    public string? AzureAIProjectName { get; init; }
    public string? AzureLocation { get; init; }
    public string? AzureSubscriptionId { get; init; }
    public string? AzureTenantId { get; init; }
    public string AzureEmbeddingModelName { get; init; } = "text-embedding-ada-002";
    public string AzureTextModelName { get; init; } = "gpt-4o";
    public string? AzureAISearchEndpoint { get; init; }
    public string? AzureAISearchAdminKey { get; init; }
    public string? OtelExporterOtlpEndpoint { get; init; }
    public string? ApplicationInsightsConnectionString { get; init; }

    // Cosmos DB configuration
    public string? CosmosDbEndpoint { get; init; }
    public string? CosmosDbConnectionString { get; init; }
    public string? CosmosDbDatabaseName { get; init; }
    public string? CosmosDbChatHistoryContainer { get; init; }
    public string? CosmosDbUserProfileContainer { get; init; }
    public string? CosmosDbFlightsContainer { get; init; }

    // MCP tool configuration for flight search
    public string McpFlightSearchToolBaseUrl { get; init; } = "http://localhost:5002";
    public string McpFlightSearchApiKey { get; init; }
}