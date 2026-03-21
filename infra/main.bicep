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

// Load standard Azure abbreviations
var abbr = json(loadTextContent('./abbreviations.json'))

// Resource naming convention
var rgName = '${abbr.resourceGroups}${resourcePrefix}-${environmentName}'
var uniqueSuffixValue = substring(uniqueString(subscription().subscriptionId, rgName), 0, 6)

// Resource names
var resourceNames = {
  aiService: toLower('${abbr.aiServicesAccounts}${uniqueSuffixValue}')
  aiFoundryAccount: toLower('${abbr.aiFoundryAccounts}${uniqueSuffixValue}')
  aiFoundryProject: toLower('${abbr.aiFoundryAccounts}proj-${uniqueSuffixValue}')
  logAnalytics: toLower('log-${uniqueSuffixValue}')
  appInsights: toLower('appi-${uniqueSuffixValue}')
  cosmosDb: toLower('${abbr.cosmosDBAccounts}-${uniqueSuffixValue}')
  containerRegistry: toLower('${abbr.containerRegistryRegistries}${replace(uniqueSuffixValue, '-', '')}')
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

// Deploy shared resources (Log Analytics and Application Insights)
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

// Deploy Azure Container Registry
module containerRegistry 'modules/container-registry.bicep' = {
  scope: rg
  name: 'acr-${uniqueSuffixValue}'
  params: {
    name: resourceNames.containerRegistry
    location: location
    tags: tags
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

// Container image parameters (set by azd after build)
param backendImageName string = ''
param frontendImageName string = ''
param mcpImageName string = ''

// Deploy Container Apps (Frontend and Backend)
module app 'modules/app.bicep' = {
  scope: rg
  name: 'app-${uniqueSuffixValue}'
  params: {
    resourcePrefix: resourcePrefix
    uniqueSuffixValue: uniqueSuffixValue
    location: location
    tags: tags
    foundryProjectEndpoint: aiProject.outputs.endpoint
    foundryProjectName: aiProject.outputs.name
    openAIDeploymentName: chatCompletionModel
    appInsightsConnectionString: shared.outputs.appInsightsConnectionString
    cosmosDbEndpoint: cosmosDb.outputs.cosmosDbEndpoint
    cosmosDbConnectionString: cosmosDb.outputs.cosmosDbConnectionString
    cosmosDbDatabaseName: cosmosDb.outputs.cosmosDbDatabaseName
    chatHistoryContainerName: cosmosDb.outputs.chatHistoryContainerName
    aiServicesEndpoint: aiFoundryAccount.outputs.endpoint
    aiServicesKey: aiFoundryAccount.outputs.apiKey
    aiFoundryServiceEndpoint: 'https://${aiFoundryAccount.outputs.name}.services.ai.azure.com/'
    logAnalyticsWorkspaceId: shared.outputs.logAnalyticsWorkspaceId
    containerRegistryName: containerRegistry.outputs.name
    backendImageName: backendImageName
    frontendImageName: frontendImageName
    mcpImageName: mcpImageName
    embeddingModelName: embeddingModelName
  }
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_SUBSCRIPTION_ID string = subscription().subscriptionId
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = shared.outputs.logAnalyticsWorkspaceName
output AZURE_APP_INSIGHTS_NAME string = shared.outputs.appInsightsName
output AZURE_APP_INSIGHTS_CONNECTION_STRING string = shared.outputs.appInsightsConnectionString
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name

output AZURE_AI_PROJECT_NAME string = aiProject.outputs.name
output AZURE_AI_PROJECT_ENDPOINT string = aiProject.outputs.endpoint
output AZURE_AI_FOUNDRY_SERVICE_ENDPOINT string = 'https://${aiFoundryAccount.outputs.name}.services.ai.azure.com/'
output AZURE_AI_SERVICES_ENDPOINT string = aiFoundryAccount.outputs.endpoint
output AZURE_AI_SERVICES_KEY string = aiFoundryAccount.outputs.apiKey

output BACKEND_APP_URL string = app.outputs.BACKEND_APP_URL
output FRONTEND_APP_URL string = app.outputs.FRONTEND_APP_URL
output MCP_SERVER_URL string = app.outputs.MCP_SERVER_URL

output AZURE_OPENAI_DEPLOYMENT_NAME string = chatCompletionModel
output AZURE_TEXT_MODEL_NAME string = chatCompletionModel //TODO: to be removed when the notebook is updated
output AZURE_EMBEDDING_MODEL_NAME string = embeddingModelName

output COSMOS_DB_ENDPOINT string = cosmosDb.outputs.cosmosDbEndpoint
output COSMOS_DB_CONNECTION_STRING string = cosmosDb.outputs.cosmosDbConnectionString
output COSMOS_DB_DATABASE_NAME string = cosmosDb.outputs.cosmosDbDatabaseName
output COSMOS_DB_CHAT_HISTORY_CONTAINER string = cosmosDb.outputs.chatHistoryContainerName
