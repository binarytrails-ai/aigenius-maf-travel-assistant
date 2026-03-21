@description('Azure region of the deployment')
param location string

@description('Tags to add to the resources')
param tags object

@description('AI hub name')
param aiHubName string

@description('AI hub display name')
param aiHubFriendlyName string = aiHubName

@description('AI hub description')
param aiHubDescription string

@description('Resource ID of the storage account resource for storing experimentation outputs')
param storageAccountId string

param keyVaultId string

@description('Resource ID of the OpenAI Services resource')
param openAiServiceId string

@description('Endpoint of the OpenAI Services resource')
param openAiServiceEndpoint string

// @description('Model/AI Resource deployment location')
// param modelLocation string

param aiSearchName string

param aiSearchId string

@description('Endpoint of the AI Search service')
param aiSearchEndpoint string

@description('Resource ID of the Application Insights instance')
param appInsightsResourceId string

resource searchService 'Microsoft.Search/searchServices@2024-06-01-preview' existing = {
  name: aiSearchName
}
resource aiHub 'Microsoft.MachineLearningServices/workspaces@2025-04-01' = {
  name: aiHubName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    // organization
    friendlyName: aiHubFriendlyName
    description: aiHubDescription

    // dependent resources
    keyVault: keyVaultId
    storageAccount: storageAccountId
    applicationInsights: appInsightsResourceId
  }
  kind: 'hub'

  resource openAiServiceConnection 'connections@2024-04-01-preview' = {
    name: 'connection-AIServices'
    properties: {
      category: 'AIServices'
      target: openAiServiceEndpoint
      authType: 'ApiKey'
      isSharedToAll: true
      credentials: {
        key: listKeys(openAiServiceId, '2023-05-01').key1
      }
      metadata: {
        ApiType: 'Azure'
        ResourceId: openAiServiceId
        //Location: modelLocation
      }
    }
  }

  // https://learn.microsoft.com/en-us/azure/templates/microsoft.machinelearningservices/2024-01-01-preview/workspaces/connections?pivots=deployment-language-bicep
  //   resource aiSearchConnection 'connections@2024-04-01-preview' = {
  //     name: 'connection-AISearch'
  //     properties: {
  //       category: 'AISearch'
  //       target: aiSearchEndpoint
  //       authType: 'ApiKey'
  //       isSharedToAll: true
  //       credentials: {
  //         key: listAdminKeys(aiSearchId, '2023-11-01').primaryKey
  //       }
  //       metadata: {
  //         ApiType: 'Azure'
  //         ResourceId: aiSearchId
  //       }
  //     }
  //   }

  //https://github.com/Azure/azure-quickstart-templates/blob/master/quickstarts/microsoft.azure-ai-agent-service/standard-agent/modules-standard/standard-ai-hub.bicep
  resource aiSearchConnection 'connections@2024-04-01-preview' = {
    name: 'connection-AISearch'
    properties: {
      category: 'CognitiveSearch'
      target: 'https://${aiSearchName}.search.windows.net'
      authType: 'ApiKey'
      isSharedToAll: true
      credentials: {
        key: listAdminKeys(aiSearchId, '2023-11-01').primaryKey
      }
      metadata: {
        ApiType: 'Azure'
        ResourceId: aiSearchId
        location: searchService.location
      }
    }
  }
}

output aiHubId string = aiHub.id
output aiHubName string = aiHub.name
