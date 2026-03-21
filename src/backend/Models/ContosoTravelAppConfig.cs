namespace ContosoTravelAgent.Host.Models;

public record ContosoTravelAppConfig
{
    public bool UseGitHubModels { get; init; } = true;

    // GitHub Models configuration
    public string? GithubToken { get; init; }
    public string GithubModelsBaseUrl { get; init; } = "https://models.inference.ai.azure.com";
    public string GithubTextModelId { get; init; } = "gpt-4o";
    public string GithubEmbeddingModelId { get; init; } = "openai/text-embedding-ada-002";

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

    public string Mem0Endpoint { get; init; } = "https://api.mem0.ai";
    public string? Mem0ApiKey { get; init; }
}