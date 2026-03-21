param name string
param location string = resourceGroup().location
param tags object = {}

@description('Indicates whether admin user is enabled')
param adminUserEnabled bool = false

@description('Public network access setting')
param publicNetworkAccess string = 'Enabled'

@description('SKU settings')
param sku object = {
  name: 'Basic'
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  tags: tags
  sku: sku
  properties: {
    adminUserEnabled: adminUserEnabled
    publicNetworkAccess: publicNetworkAccess
  }
}

output loginServer string = containerRegistry.properties.loginServer
output name string = containerRegistry.name
output id string = containerRegistry.id
