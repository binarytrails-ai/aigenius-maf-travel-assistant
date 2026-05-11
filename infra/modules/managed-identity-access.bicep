@description('Azure AI Foundry account name')
param aiFoundryAccountName string

@description('Cosmos DB account name')
param cosmosDbAccountName string

@description('Backend managed identity name')
param backendIdentityName string

@description('Backend managed identity principal ID')
param backendPrincipalId string

@description('MCP managed identity name')
param mcpIdentityName string

@description('MCP managed identity principal ID')
param mcpPrincipalId string

resource aiFoundryAccount 'Microsoft.CognitiveServices/accounts@2025-04-01-preview' existing = {
  name: aiFoundryAccountName
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosDbAccountName
}

resource backendFoundryRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, aiFoundryAccountName, backendIdentityName, 'backend-cognitive-services-user')
  scope: aiFoundryAccount
  properties: {
    principalType: 'ServicePrincipal'
    principalId: backendPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
  }
}

resource mcpFoundryRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, aiFoundryAccountName, mcpIdentityName, 'mcp-cognitive-services-user')
  scope: aiFoundryAccount
  properties: {
    principalType: 'ServicePrincipal'
    principalId: mcpPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
  }
}

resource backendCosmosDataRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmosDbAccount
  name: guid(resourceGroup().id, cosmosDbAccountName, backendIdentityName, 'backend-cosmos-data-contributor')
  properties: {
    roleDefinitionId: '${cosmosDbAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: backendPrincipalId
    scope: cosmosDbAccount.id
  }
}

resource mcpCosmosDataRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmosDbAccount
  name: guid(resourceGroup().id, cosmosDbAccountName, mcpIdentityName, 'mcp-cosmos-data-contributor')
  properties: {
    roleDefinitionId: '${cosmosDbAccount.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: mcpPrincipalId
    scope: cosmosDbAccount.id
  }
}
