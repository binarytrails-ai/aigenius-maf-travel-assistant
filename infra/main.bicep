targetScope = 'subscription'

// Core parameters
@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string = 'australiaeast'

param resourcePrefix string = 'aiagent-ws'

// Azure AI Service parameters
param chatCompletionModel string = 'gpt-4o'
param chatCompletionModelFormat string = 'OpenAI'
param chatCompletionModelVersion string = '2024-11-20'
param chatCompletionModelSkuName string = 'GlobalStandard'
param chatCompletionModelCapacity int = 50
param modelLocation string = 'australiaeast'

// Embedding model parameters
param embeddingModelName string = 'text-embedding-ada-002'
param embeddingModelFormat string = 'OpenAI'
param embeddingModelVersion string = '2'
param embeddingModelSkuName string = 'GlobalStandard'
param embeddingModelCapacity int = 120

// MCP Server authentication
@secure()
@description('API key for MCP Flight Search server authentication')
param mcpFlightSearchApiKey string

// Load standard Azure abbreviations
var abbr = json(loadTextContent('./abbreviations.json'))

// Resource naming convention
var rgName = '${abbr.resourceGroups}${resourcePrefix}-${environmentName}'
var uniqueSuffixValue = substring(uniqueString(subscription().subscriptionId, rgName), 0, 6)

// Resource names
var resourceNames = {
  aiService: toLower('${abbr.aiServicesAccounts}${uniqueSuffixValue}')
  keyVault: toLower('${abbr.keyVault}${uniqueSuffixValue}')
  storageAccount: toLower('${abbr.storageStorageAccounts}${replace(uniqueSuffixValue, '-', '')}')
  aiFoundryAccount: toLower('${abbr.aiFoundryAccounts}${uniqueSuffixValue}')
  aiFoundryProject: toLower('${abbr.aiFoundryAccounts}proj-${uniqueSuffixValue}')
  aiSearch: toLower('${abbr.aiSearchSearchServices}${replace(uniqueSuffixValue, '-', '')}')
  logAnalytics: toLower('log-${uniqueSuffixValue}')
  appInsights: toLower('appi-${uniqueSuffixValue}')
  cosmosDb: toLower('${abbr.cosmosDBAccounts}-${uniqueSuffixValue}')
  containerRegistry: toLower('${abbr.containerRegistryRegistries}${uniqueSuffixValue}')
  containerAppsEnvironment: toLower('${abbr.appManagedEnvironments}${uniqueSuffixValue}')
  backendIdentity: toLower('id-backend-${uniqueSuffixValue}')
  mcpIdentity: toLower('id-mcp-${uniqueSuffixValue}')
  frontendIdentity: toLower('id-frontend-${uniqueSuffixValue}')
}

// Tags
var tags = {
  'azd-env-name': environmentName
  'azd-service-name': 'aiagent'
}

// Resource group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: rgName
  location: location
  tags: tags
}

// Deploy shared resources (Log Analytics, App Insights)
module shared 'modules/shared.bicep' = {
  scope: rg
  name: 'shared-${uniqueSuffixValue}'
  params: {
    location: location
    tags: tags
    logAnalyticsWorkspaceName: resourceNames.logAnalytics
    appInsightsName: resourceNames.appInsights
  }
}

//Create AI Foundry Account
module aiFoundryAccount 'modules/ai-foundry-account.bicep' = {
  scope: rg
  name: 'foundry-${uniqueSuffixValue}'
  params: {
    name: resourceNames.aiFoundryAccount
    location: location
    tags: tags
  }
}

// Create AI Foundry Project
module aiProject 'modules/ai-project.bicep' = {
  scope: rg
  name: 'proj-${uniqueSuffixValue}'
  params: {
    name: resourceNames.aiFoundryProject
    location: location
    tags: tags
    aiFoundryName: aiFoundryAccount.outputs.name
  }
}

// Create the OpenAI Service
module aiDependencies 'modules/ai-services.bicep' = {
  scope: rg
  name: 'dep-${uniqueSuffixValue}'
  params: {
    aiServicesName: resourceNames.aiService
    location: location
    tags: tags

    aiFoundryAccountName: aiFoundryAccount.outputs.name
    // Model deployment parameters
    modelName: chatCompletionModel
    modelFormat: chatCompletionModelFormat
    modelVersion: chatCompletionModelVersion
    modelSkuName: chatCompletionModelSkuName
    modelCapacity: chatCompletionModelCapacity
    modelLocation: modelLocation

    // Embedding model parameters
    embeddingModelName: embeddingModelName
    embeddingModelFormat: embeddingModelFormat
    embeddingModelVersion: embeddingModelVersion
    embeddingModelSkuName: embeddingModelSkuName
    embeddingModelCapacity: embeddingModelCapacity
  }
}

// Deploy Cosmos DB for chat history and user profiles
module cosmosDb 'modules/cosmos-db.bicep' = {
  scope: rg
  name: 'cosmos-${uniqueSuffixValue}'
  params: {
    cosmosDbAccountName: resourceNames.cosmosDb
    cosmosDbDatabaseName: 'ContosoTravelDb'
    chatHistoryContainerName: 'ChatHistory'
    location: location
    tags: tags
    vectorDimensions: 3072
  }
}

// Deploy Azure Container Registry for container images
module containerRegistry 'modules/container-registry.bicep' = {
  scope: rg
  name: 'acr-${uniqueSuffixValue}'
  params: {
    name: resourceNames.containerRegistry
    location: location
    tags: tags
    adminUserEnabled: false
  }
}

// Deploy Container Apps Environment
module containerAppsEnvironment 'modules/container-apps-environment.bicep' = {
  scope: rg
  name: 'cae-${uniqueSuffixValue}'
  params: {
    name: resourceNames.containerAppsEnvironment
    location: location
    tags: tags
  }
}

// Deploy Managed Identity for Backend App
module backendIdentity 'modules/managed-identity.bicep' = {
  scope: rg
  name: 'id-backend-${uniqueSuffixValue}'
  params: {
    name: resourceNames.backendIdentity
    location: location
    tags: tags
  }
}

// Deploy Managed Identity for MCP Server App
module mcpIdentity 'modules/managed-identity.bicep' = {
  scope: rg
  name: 'id-mcp-${uniqueSuffixValue}'
  params: {
    name: resourceNames.mcpIdentity
    location: location
    tags: tags
  }
}

// Deploy Managed Identity for Frontend App
module frontendIdentity 'modules/managed-identity.bicep' = {
  scope: rg
  name: 'id-frontend-${uniqueSuffixValue}'
  params: {
    name: resourceNames.frontendIdentity
    location: location
    tags: tags
  }
}


// Deploy Backend Container App
module backendApp 'modules/containerapp.bicep' = {
  scope: rg
  name: 'backend-${uniqueSuffixValue}'
  params: {
    name: '${resourcePrefix}-backend-${uniqueSuffixValue}'
    location: location
    tags: union(tags, {
      'azd-service-name': 'backend'
    })
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    identityName: backendIdentity.outputs.name
    identityType: 'UserAssigned'
    targetPort: 8080
    external: true
    containerCpuCoreCount: '1.0'
    containerMemory: '2.0Gi'
    env: [
      {
        name: 'USE_GITHUB_MODELS'
        value: 'false'
      }
      {
        name: 'AZURE_AI_PROJECT_ENDPOINT'
        value: aiProject.outputs.endpoint
      }
      {
        name: 'AZURE_AI_PROJECT_NAME'
        value: aiProject.outputs.name
      }
      {
        name: 'AZURE_AI_FOUNDRY_SERVICE_ENDPOINT'
        value: 'https://${aiFoundryAccount.outputs.name}.services.ai.azure.com/'
      }
      {
        name: 'AZURE_AI_SERVICES_ENDPOINT'
        value: aiFoundryAccount.outputs.endpoint
      }
      {
        name: 'AZURE_AI_SERVICES_KEY'
        value: aiFoundryAccount.outputs.apiKey
      }
      {
        name: 'AZURE_OPENAI_DEPLOYMENT_NAME'
        value: chatCompletionModel
      }
      {
        name: 'AZURE_LOCATION'
        value: location
      }
      {
        name: 'AZURE_TENANT_ID'
        value: tenant().tenantId
      }
      {
        name: 'AZURE_SUBSCRIPTION_ID'
        value: subscription().subscriptionId
      }
      {
        name: 'COSMOS_DB_ENDPOINT'
        value: cosmosDb.outputs.cosmosDbEndpoint
      }
      {
        name: 'COSMOS_DB_CONNECTION_STRING'
        value: cosmosDb.outputs.cosmosDbConnectionString
      }
      {
        name: 'COSMOS_DB_DATABASE_NAME'
        value: cosmosDb.outputs.cosmosDbDatabaseName
      }
      {
        name: 'COSMOS_DB_CHAT_HISTORY_CONTAINER'
        value: cosmosDb.outputs.chatHistoryContainerName
      }
      {
        name: 'COSMOS_DB_USER_PROFILE_CONTAINER'
        value: cosmosDb.outputs.userProfileContainerName
      }
      {
        name: 'MCP_FLIGHT_SEARCH_TOOL_BASE_URL'
        value: mcpServerApp.outputs.uri
      }
      {
        name: 'MCP_FLIGHT_SEARCH_API_KEY'
        value: mcpFlightSearchApiKey
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: shared.outputs.appInsightsConnectionString
      }
      {
        name: 'PORT'
        value: '8080'
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
    ]
  }
}

// Deploy MCP Server Container App
module mcpServerApp 'modules/containerapp.bicep' = {
  scope: rg
  name: 'mcp-${uniqueSuffixValue}'
  params: {
    name: '${resourcePrefix}-mcp-${uniqueSuffixValue}'
    location: location
    tags: union(tags, {
      'azd-service-name': 'mcp-server'
    })
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    identityName: mcpIdentity.outputs.name
    identityType: 'UserAssigned'
    targetPort: 8080
    external: true
    containerCpuCoreCount: '0.5'
    containerMemory: '1.0Gi'
    env: [
      {
        name: 'USE_GITHUB_MODELS'
        value: 'false'
      }
      {
        name: 'AZURE_AI_PROJECT_ENDPOINT'
        value: aiProject.outputs.endpoint
      }
      {
        name: 'AZURE_AI_PROJECT_NAME'
        value: aiProject.outputs.name
      }
      {
        name: 'AZURE_AI_FOUNDRY_SERVICE_ENDPOINT'
        value: 'https://${aiFoundryAccount.outputs.name}.services.ai.azure.com/'
      }
      {
        name: 'AZURE_AI_SERVICES_ENDPOINT'
        value: aiFoundryAccount.outputs.endpoint
      }
      {
        name: 'AZURE_AI_SERVICES_KEY'
        value: aiFoundryAccount.outputs.apiKey
      }
      {
        name: 'AZURE_OPENAI_DEPLOYMENT_NAME'
        value: chatCompletionModel
      }
      {
        name: 'AZURE_LOCATION'
        value: location
      }
      {
        name: 'AZURE_TENANT_ID'
        value: tenant().tenantId
      }
      {
        name: 'AZURE_SUBSCRIPTION_ID'
        value: subscription().subscriptionId
      }
      {
        name: 'COSMOS_DB_ENDPOINT'
        value: cosmosDb.outputs.cosmosDbEndpoint
      }
      {
        name: 'COSMOS_DB_CONNECTION_STRING'
        value: cosmosDb.outputs.cosmosDbConnectionString
      }
      {
        name: 'COSMOS_DB_DATABASE_NAME'
        value: cosmosDb.outputs.cosmosDbDatabaseName
      }
      {
        name: 'COSMOS_DB_CHAT_HISTORY_CONTAINER'
        value: cosmosDb.outputs.chatHistoryContainerName
      }
      {
        name: 'MCP_FLIGHT_SEARCH_API_KEY'
        value: mcpFlightSearchApiKey
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: shared.outputs.appInsightsConnectionString
      }
      {
        name: 'PORT'
        value: '8080'
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
    ]
  }
}

// Deploy Frontend Container App
module frontendApp 'modules/containerapp.bicep' = {
  scope: rg
  name: 'frontend-${uniqueSuffixValue}'
  params: {
    name: '${resourcePrefix}-frontend-${uniqueSuffixValue}'
    location: location
    tags: union(tags, {
      'azd-service-name': 'frontend'
    })
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    identityName: frontendIdentity.outputs.name
    identityType: 'UserAssigned'
    targetPort: 3000
    external: true
    containerCpuCoreCount: '0.5'
    containerMemory: '1.0Gi'
    env: [
      {
        name: 'BACKEND_AGENT_BASE_URL'
        value: backendApp.outputs.uri
      }
      {
        name: 'NODE_ENV'
        value: 'production'
      }
      {
        name: 'PORT'
        value: '3000'
      }
      {
        name: 'HOSTNAME'
        value: '0.0.0.0'
      }
    ]
  }
}


output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_SUBSCRIPTION_ID string = subscription().subscriptionId
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = shared.outputs.logAnalyticsWorkspaceName
output AZURE_APP_INSIGHTS_NAME string = shared.outputs.appInsightsName
output AZURE_APP_INSIGHTS_CONNECTION_STRING string = shared.outputs.appInsightsConnectionString

output AZURE_AI_PROJECT_NAME string = aiProject.outputs.name
output AZURE_AI_PROJECT_ENDPOINT string = aiProject.outputs.endpoint
output AZURE_AI_FOUNDRY_SERVICE_ENDPOINT string = 'https://${aiFoundryAccount.outputs.name}.services.ai.azure.com/'
output AZURE_AI_SERVICES_ENDPOINT string = aiFoundryAccount.outputs.endpoint
output AZURE_AI_SERVICES_KEY string = aiFoundryAccount.outputs.apiKey

output BACKEND_URI string = backendApp.outputs.uri
output BACKEND_APP_URL string = backendApp.outputs.uri

output MCP_SERVER_URI string = mcpServerApp.outputs.uri
output MCP_SERVER_APP_URL string = mcpServerApp.outputs.uri
output MCP_FLIGHT_SEARCH_TOOL_BASE_URL string = mcpServerApp.outputs.uri

output FRONTEND_URI string = frontendApp.outputs.uri
output FRONTEND_APP_URL string = frontendApp.outputs.uri


output AZURE_OPENAI_DEPLOYMENT_NAME string = chatCompletionModel
output AZURE_TEXT_MODEL_NAME string = chatCompletionModel //TODO: to be removed when the notebook is updated
output AZURE_EMBEDDING_MODEL_NAME string = embeddingModelName

output COSMOS_DB_ENDPOINT string = cosmosDb.outputs.cosmosDbEndpoint
output COSMOS_DB_CONNECTION_STRING string = cosmosDb.outputs.cosmosDbConnectionString
output COSMOS_DB_DATABASE_NAME string = cosmosDb.outputs.cosmosDbDatabaseName
output COSMOS_DB_CHAT_HISTORY_CONTAINER string = cosmosDb.outputs.chatHistoryContainerName
output COSMOS_DB_USER_PROFILE_CONTAINER string = cosmosDb.outputs.userProfileContainerName

output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppsEnvironment.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppsEnvironment.outputs.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppsEnvironment.outputs.defaultDomain
