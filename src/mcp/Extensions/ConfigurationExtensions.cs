using Azure.Identity;
using ContosoTravel.McpServer.Models;
using ContosoTravel.ServiceDefaults;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace ContosoTravel.McpServer.Extensions;

/// <summary>
/// Extension methods for loading and registering MCP server configuration.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Loads the MCP server configuration from environment variables and IConfiguration,
    /// and registers all required services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The loaded AppConfig instance.</returns>
    public static AppConfig LoadMcpServerConfig(this IServiceCollection services, IConfiguration configuration)
    {
        EnvironmentVariableHelper.LoadEnvironmentVariables();

        var useGitHubModels = EnvironmentVariableHelper.GetBoolConfigValue("USE_GITHUB_MODELS", configuration);

        // GitHub Models configuration
        var githubToken = EnvironmentVariableHelper.GetConfigValue("GITHUB_TOKEN", configuration);
        var githubModelsBaseUrl = EnvironmentVariableHelper.GetConfigValue("GITHUB_MODELS_BASE_URL", configuration, "https://models.inference.ai.azure.com");
        var githubEmbeddingModelId = EnvironmentVariableHelper.GetConfigValue("GITHUB_EMBEDDING_MODEL_ID", configuration, "openai/text-embedding-ada-002");

        // Azure AI configuration
        var azureAiServicesEndpoint = EnvironmentVariableHelper.GetConfigValue("AZURE_AI_SERVICES_ENDPOINT", configuration);
        var azureAiServicesKey = EnvironmentVariableHelper.GetConfigValue("AZURE_AI_SERVICES_KEY", configuration);
        var embeddingModelName = EnvironmentVariableHelper.GetConfigValue("AZURE_EMBEDDING_MODEL_NAME", configuration, "text-embedding-ada-002");

        // Cosmos DB configuration
        var cosmosDbEndpoint = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_ENDPOINT", configuration);
        var cosmosDbConnectionString = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_CONNECTION_STRING", configuration);
        var cosmosDbDatabaseName = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_DATABASE_NAME", configuration, "ContosoTravel");
        var cosmosDbFlightsContainer = EnvironmentVariableHelper.GetConfigValue("COSMOS_DB_FLIGHTS_CONTAINER", configuration, "Flights");

        // Validate configuration based on mode
        if (useGitHubModels && string.IsNullOrWhiteSpace(githubToken))
        {
            throw new InvalidOperationException(
                "GitHub Models token not found. Set GITHUB_TOKEN in .env file.");
        }

        if (!useGitHubModels && string.IsNullOrWhiteSpace(azureAiServicesEndpoint))
        {
            throw new InvalidOperationException(
                "Azure AI Services endpoint not found. Set AZURE_AI_SERVICES_ENDPOINT in .env file.");
        }

        if (string.IsNullOrWhiteSpace(cosmosDbConnectionString) && string.IsNullOrWhiteSpace(cosmosDbEndpoint))
        {
            throw new InvalidOperationException(
                "Cosmos DB configuration is missing. Set either COSMOS_DB_CONNECTION_STRING or COSMOS_DB_ENDPOINT in .env file.");
        }

        var config = new AppConfig
        {
            UseGitHubModels = useGitHubModels,
            GithubToken = githubToken,
            GithubModelsBaseUrl = githubModelsBaseUrl,
            GithubEmbeddingModelId = githubEmbeddingModelId,
            AzureAIServicesEndpoint = azureAiServicesEndpoint,
            AzureAIServicesKey = azureAiServicesKey,
            AzureEmbeddingModelName = embeddingModelName,
            CosmosDbEndpoint = cosmosDbEndpoint,
            CosmosDbConnectionString = cosmosDbConnectionString,
            CosmosDbDatabaseName = cosmosDbDatabaseName,
            CosmosDbFlightsContainer = cosmosDbFlightsContainer
        };

        services.AddSingleton<IOptions<AppConfig>>(Options.Create(config));
        services.AddSingleton(config);

        return config;
    }

    /// <summary>
    /// Registers Cosmos DB services based on the provided configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The application configuration.</param>
    public static void AddCosmosDb(this IServiceCollection services, AppConfig config)
    {
        if (!string.IsNullOrEmpty(config.CosmosDbConnectionString))
        {
            var cosmosClient = new CosmosClient(config.CosmosDbConnectionString);
            var database = cosmosClient.GetDatabase(config.CosmosDbDatabaseName);
            services.AddSingleton(database);
        }
        else
        {
            var cosmosClient = new CosmosClient(config.CosmosDbEndpoint, new DefaultAzureCredential());
            var database = cosmosClient.GetDatabase(config.CosmosDbDatabaseName);
            services.AddSingleton(database);
        }
    }

    /// <summary>
    /// Registers the embedding client based on the provided configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The application configuration.</param>
    public static void AddEmbeddingClient(this IServiceCollection services, AppConfig config)
    {
        EmbeddingClient embeddingClient;

        if (config.UseGitHubModels)
        {
            var apiKeyCredential = new System.ClientModel.ApiKeyCredential(config.GithubToken!);
            var openAiClient = new OpenAI.OpenAIClient(apiKeyCredential, new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(config.GithubModelsBaseUrl)
            });
            embeddingClient = openAiClient.GetEmbeddingClient(config.GithubEmbeddingModelId);
        }
        else
        {
            var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(
                new Uri(config.AzureAIServicesEndpoint!),
                new DefaultAzureCredential());
            embeddingClient = azureClient.GetEmbeddingClient(config.AzureEmbeddingModelName);
        }

        services.AddSingleton(embeddingClient);
    }
}
