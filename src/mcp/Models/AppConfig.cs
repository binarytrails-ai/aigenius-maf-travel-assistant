namespace ContosoTravel.McpServer.Models;

/// <summary>
/// Application configuration for the MCP server
/// </summary>
public record AppConfig
{
    // Azure AI configuration
    public string? AzureAIServicesEndpoint { get; init; }
    public string? AzureAIServicesKey { get; init; }
    public string AzureEmbeddingModelName { get; init; } = "text-embedding-ada-002";

    // Observability configuration
    public string? OtelExporterOtlpEndpoint { get; init; }
    public string? ApplicationInsightsConnectionString { get; init; }

    // Cosmos DB configuration
    public string? CosmosDbEndpoint { get; init; }
    public string? CosmosDbConnectionString { get; init; }
    public string CosmosDbDatabaseName { get; init; } = "ContosoTravel";
    public string CosmosDbFlightsContainer { get; init; } = "Flights";
}
