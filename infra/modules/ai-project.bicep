@description('Azure region of the deployment')
param location string

@description('Tags to add to the resources')
param tags object = {}

@description('AI Project name')
param name string

@description('Name of the Azure AI Foundry resource (parent account)')
param aiFoundryName string

resource aiFoundry 'Microsoft.CognitiveServices/accounts@2025-04-01-preview' existing = {
  name: aiFoundryName
}

resource aiProject 'Microsoft.CognitiveServices/accounts/projects@2025-04-01-preview' = {
  name: name
  parent: aiFoundry
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: name
  }
}

output resourceId string = aiProject.id
output name string = aiProject.name
output aiProjectPrincipalId string = aiProject.identity.principalId
output endpoint string = 'https://${aiFoundry.name}.services.ai.azure.com/api/projects/${aiProject.name}'
