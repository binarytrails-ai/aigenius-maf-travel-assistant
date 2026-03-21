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
param logAnalyticsWorkspaceId string = ''
param containerRegistryName string = ''
param backendImageName string = ''
param frontendImageName string = ''
param mcpImageName string = ''
param embeddingModelName string = 'text-embedding-ada-002'

var frontendAppName = '${resourcePrefix}-web-${uniqueSuffixValue}'
var backendAppName = '${resourcePrefix}-api-${uniqueSuffixValue}'
var mcpAppName = '${resourcePrefix}-mcp-${uniqueSuffixValue}'
var containerAppEnvName = '${resourcePrefix}-env-${uniqueSuffixValue}'

// Reference to existing ACR
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

// Container App Environment
resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppEnvName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2022-10-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2022-10-01').primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

// Backend Container App
resource backendApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: backendAppName
  location: location
  tags: union(tags, {
    'azd-service-name': 'backend'
  })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: 'system'
        }
      ]
      ingress: {
        external: true
        targetPort: !empty(backendImageName) ? 8080 : 80
        transport: 'auto'
        allowInsecure: false
        corsPolicy: {
          allowedOrigins: [
            'https://${frontendAppName}.${containerAppEnv.properties.defaultDomain}'
            '*'
          ]
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          allowedHeaders: ['*']
          allowCredentials: true
        }
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'backend'
          image: !empty(backendImageName) ? backendImageName : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
          probes: [
            {
              type: 'liveness'
              httpGet: {
                path: '/health'
                port: !empty(backendImageName) ? 8080 : 80
              }
              initialDelaySeconds: 30
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'readiness'
              httpGet: {
                path: '/health'
                port: !empty(backendImageName) ? 8080 : 80
              }
              initialDelaySeconds: 5
              periodSeconds: 5
              failureThreshold: 3
            }
          ]
          env: [
            {
              name: 'USE_GITHUB_MODELS'
              value: 'false'
            }
            {
              name: 'FRONTEND_APP_URL'
              value: 'https://${frontendAppName}.${containerAppEnv.properties.defaultDomain}'
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
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

// Frontend Container App
resource frontendApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: frontendAppName
  location: location
  tags: union(tags, {
    'azd-service-name': 'frontend'
  })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: 'system'
        }
      ]
      ingress: {
        external: true
        targetPort: !empty(frontendImageName) ? 3000 : 80
        transport: 'auto'
        allowInsecure: false
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: !empty(frontendImageName) ? frontendImageName : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          probes: [
            {
              type: 'liveness'
              httpGet: {
                path: '/'
                port: !empty(frontendImageName) ? 3000 : 80
              }
              initialDelaySeconds: 30
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'readiness'
              httpGet: {
                path: '/'
                port: !empty(frontendImageName) ? 3000 : 80
              }
              initialDelaySeconds: 5
              periodSeconds: 5
              failureThreshold: 3
            }
          ]
          env: [
            {
              name: 'VITE_API_BASE_URL'
              value: 'https://${backendApp.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

// MCP Server Container App
resource mcpApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: mcpAppName
  location: location
  tags: union(tags, {
    'azd-service-name': 'mcp'
  })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: 'system'
        }
      ]
      ingress: {
        external: true
        targetPort: !empty(mcpImageName) ? 8080 : 80
        transport: 'auto'
        allowInsecure: false
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'mcp'
          image: !empty(mcpImageName) ? mcpImageName : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          probes: [
            {
              type: 'liveness'
              httpGet: {
                path: '/health'
                port: !empty(mcpImageName) ? 8080 : 80
              }
              initialDelaySeconds: 30
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'readiness'
              httpGet: {
                path: '/health'
                port: !empty(mcpImageName) ? 8080 : 80
              }
              initialDelaySeconds: 5
              periodSeconds: 5
              failureThreshold: 3
            }
          ]
          env: [
            {
              name: 'USE_GITHUB_MODELS'
              value: 'false'
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
              name: 'AZURE_EMBEDDING_MODEL_NAME'
              value: embeddingModelName
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
              name: 'COSMOS_DB_FLIGHTS_CONTAINER'
              value: 'Flights'
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
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
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

// Role assignments for ACR Pull (AcrPull role)
resource backendAcrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(backendApp.id, containerRegistry.id, 'acrpull')
  scope: containerRegistry
  properties: {
    principalType: 'ServicePrincipal'
    principalId: backendApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource frontendAcrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(frontendApp.id, containerRegistry.id, 'acrpull')
  scope: containerRegistry
  properties: {
    principalType: 'ServicePrincipal'
    principalId: frontendApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

// Role assignments for MCP app
resource mcpAppRoleAssignment1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(mcpApp.id, 'mcp-role-cognitive-services-user')
  scope: resourceGroup()
  properties: {
    principalType: 'ServicePrincipal'
    principalId: mcpApp.identity.principalId
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
  }
}

resource mcpAppRoleAssignment2 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(mcpApp.id, 'mcp-role-azureai-developer')
  scope: resourceGroup()
  properties: {
    principalType: 'ServicePrincipal'
    principalId: mcpApp.identity.principalId
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '64702f94-c441-49e6-a78b-ef80e0188fee')
  }
}

resource mcpAppCosmosDbRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(mcpApp.id, 'mcp-role-cosmos-contributor')
  scope: resourceGroup()
  properties: {
    principalType: 'ServicePrincipal'
    principalId: mcpApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5bd9cd88-fe45-4216-938b-f97437e15450')
  }
}

resource mcpAcrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(mcpApp.id, containerRegistry.id, 'acrpull')
  scope: containerRegistry
  properties: {
    principalType: 'ServicePrincipal'
    principalId: mcpApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

output BACKEND_APP_URL string = 'https://${backendApp.properties.configuration.ingress.fqdn}'
output FRONTEND_APP_URL string = 'https://${frontendApp.properties.configuration.ingress.fqdn}'
output MCP_SERVER_URL string = 'https://${mcpApp.properties.configuration.ingress.fqdn}'
output CONTAINER_APP_ENVIRONMENT_ID string = containerAppEnv.id
output CONTAINER_APP_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnv.properties.defaultDomain
