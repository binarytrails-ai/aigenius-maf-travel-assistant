namespace ContosoTravel.McpServer.Models;

/// <summary>
/// Application configuration for the MCP server
/// </summary>
public record AppConfig
{
    // Azure AI configuration
    /// <summary>
    /// Gets Azure AI Services endpoint URI.
    /// </summary>
    public string? AzureAIServicesEndpoint { get; init; }

    /// <summary>
    /// Gets Azure AI Services API key.
    /// </summary>
    public string? AzureAIServicesKey { get; init; }

    /// <summary>
    /// Gets embedding model deployment name.
    /// </summary>
    public string AzureEmbeddingModelName { get; init; } = "text-embedding-3-small";

    // Observability configuration
    /// <summary>
    /// Gets OTLP exporter endpoint URI.
    /// </summary>
    public string? OtelExporterOtlpEndpoint { get; init; }

    /// <summary>
    /// Gets Application Insights connection string.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; init; }

    // Cosmos DB configuration
    /// <summary>
    /// Gets Cosmos DB account endpoint URI.
    /// </summary>
    public string? CosmosDbEndpoint { get; init; }

    /// <summary>
    /// Gets Cosmos DB connection string.
    /// </summary>
    public string? CosmosDbConnectionString { get; init; }

    /// <summary>
    /// Gets Cosmos DB database name.
    /// </summary>
    public string CosmosDbDatabaseName { get; init; } = "ContosoTravel";

    /// <summary>
    /// Gets Cosmos DB flights container name.
    /// </summary>
    public string CosmosDbFlightsContainer { get; init; } = "Flights";

    // Managed Identity configuration (required for User-assigned Managed Identity)
    /// <summary>
    /// Gets user-assigned managed identity client ID.
    /// </summary>
    public string? ManagedIdentityClientId { get; init; }
}
