param resourcePrefix string
param uniqueSuffixValue string
param location string
param tags object
param foundryProjectEndpoint string
param foundryProjectName string
param openAIDeploymentName string
param appInsightsConnectionString string = ''
param cosmosDbEndpoint string = ''
param cosmosDbDatabaseName string = ''
param cosmosDbConnectionString string = ''
param chatHistoryContainerName string = ''
param aiServicesEndpoint string = ''
param aiServicesKey string = ''
param aiFoundryServiceEndpoint string = ''

var frontendAppName = '${resourcePrefix}-web-${uniqueSuffixValue}'
var backendAppName = '${resourcePrefix}-api-${uniqueSuffixValue}'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${resourcePrefix}-plan-${uniqueSuffixValue}'
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
  tags: tags
}

// Backend App Service
resource backendApp 'Microsoft.Web/sites@2022-03-01' = {
  name: backendAppName
  location: location
  kind: 'app,linux'
  tags: union(tags, {
    'azd-service-name': 'backend'
  })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      http20Enabled: true
      appSettings: [
        {
          name: 'USE_GITHUB_MODELS'
          value: 'false'
        }
        {
          name: 'FRONTEND_APP_URL'
          value: 'https://${frontendAppName}.azurewebsites.net'
        }
        {
          name: 'AZURE_AI_PROJECT_ENDPOINT'
          value: foundryProjectEndpoint
        }
        {
          name: 'AZURE_AI_PROJECT_NAME'
          value: foundryProjectName
        }
        {
          name: 'AZURE_AI_FOUNDRY_SERVICE_ENDPOINT'
          value: aiFoundryServiceEndpoint
        }
        {
          name: 'AZURE_AI_SERVICES_ENDPOINT'
          value: aiServicesEndpoint
        }
        {
          name: 'AZURE_AI_SERVICES_KEY'
          value: aiServicesKey
        }
        {
          name: 'AZURE_OPENAI_DEPLOYMENT_NAME'
          value: openAIDeploymentName
        }
        {
          name: 'AZURE_LOCATION'
          value: location
        }
        {
          name: 'AZURE_TENANT_ID'
          value: subscription().tenantId
        }
        {
          name: 'AZURE_SUBSCRIPTION_ID'
          value: subscription().subscriptionId
        }
        {
          name: 'Azure__TenantId'
          value: subscription().tenantId
        }
        {
          name: 'Azure__SubscriptionId'
          value: subscription().subscriptionId
        }
        {
          name: 'COSMOS_DB_ENDPOINT'
          value: cosmosDbEndpoint
        }
        {
          name: 'COSMOS_DB_CONNECTION_STRING'
          value: cosmosDbConnectionString
        }
        {
          name: 'COSMOS_DB_DATABASE_NAME'
          value: cosmosDbDatabaseName
        }
        {
          name: 'COSMOS_DB_CHAT_HISTORY_CONTAINER'
          value: chatHistoryContainerName
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
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
}

// Frontend App Service
resource frontendApp 'Microsoft.Web/sites@2022-03-01' = {
  name: frontendAppName
  location: location
  kind: 'app,linux'
  tags: union(tags, {
    'azd-service-name': 'frontend'
  })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'NODE|20-lts'
      appCommandLine: 'npm start'
      appSettings: [
        {
          name: 'VITE_API_BASE_URL'
          value: 'https://${backendAppName}.azurewebsites.net'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '20-lts'
        }
      ]
    }
  }
}

// Role assignment for backend app system-assigned managed identity
resource backendAppRoleAssignment1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(backendApp.id, 'backend-role-azureai-developer')
  scope: resourceGroup()
  properties: {
    principalType: 'ServicePrincipal'
    principalId: backendApp.identity.principalId
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '64702f94-c441-49e6-a78b-ef80e0188fee')
  }
}

resource backendAppRoleAssignment2 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(backendApp.id, 'backend-role-cognitive-services-user')
  scope: resourceGroup()
  properties: {
    principalType: 'ServicePrincipal'
    principalId: backendApp.identity.principalId
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
  }
}

// Role assignment for backend to access Cosmos DB
resource backendAppCosmosDbRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(backendApp.id, 'backend-role-cosmos-contributor')
  scope: resourceGroup()
  properties: {
    principalType: 'ServicePrincipal'
    principalId: backendApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5bd9cd88-fe45-4216-938b-f97437e15450')
  }
}

output BACKEND_APP_URL string = 'https://${backendApp.name}.azurewebsites.net'
output FRONTEND_APP_URL string = 'https://${frontendApp.name}.azurewebsites.net'
