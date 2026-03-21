@description('Name of the Azure AI Foundry resource (account)')
param name string

@description('Azure region of the deployment')
param location string

@description('Tags to add to the resources')
param tags object = {}

resource aiFoundry 'Microsoft.CognitiveServices/accounts@2025-04-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    allowProjectManagement: true
    customSubDomainName: name
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
  }
}

output resourceId string = aiFoundry.id
output name string = aiFoundry.name
output endpoint string = aiFoundry.properties.endpoint
output apiKey string = aiFoundry.listKeys().key1
