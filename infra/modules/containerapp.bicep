param name string
param location string = resourceGroup().location
param tags object = {}

@description('Allowed origins')
param allowedOrigins array = []

@description('Name of the environment for container apps')
param containerAppsEnvironmentName string

@description('CPU cores allocated to a single container instance, e.g., 0.5')
param containerCpuCoreCount string = '0.5'

@description('The maximum number of replicas to run. Must be at least 1.')
@minValue(1)
param containerMaxReplicas int = 10

@description('Memory allocated to a single container instance, e.g., 1Gi')
param containerMemory string = '1.0Gi'

@description('The minimum number of replicas to run. Must be at least 1.')
param containerMinReplicas int = 1

@description('The name of the container')
param containerName string = 'main'

@description('The name of the container registry')
param containerRegistryName string = ''

@description('The environment variables for the container')
param env array = []

@description('Specifies if the resource ingress is exposed externally')
param external bool = true

@description('The name of the user-assigned identity')
param identityName string = ''

@description('The type of identity for the resource')
@allowed([ 'None', 'SystemAssigned', 'UserAssigned' ])
param identityType string = 'SystemAssigned'

@description('The name of the container image')
param imageName string = ''

@description('Specifies if Ingress is enabled for the container app')
param ingressEnabled bool = true

param revisionMode string = 'Single'

@description('The target port for the container')
param targetPort int = 80

resource userIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = if (!empty(identityName)) {
  name: identityName
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = if (!empty(containerRegistryName)) {
  name: containerRegistryName
}

// Use private registry when a container registry name is provided
var usePrivateRegistry = !empty(containerRegistryName)

// Automatically set to `UserAssigned` when an `identityName` has been set
var normalizedIdentityType = !empty(identityName) ? 'UserAssigned' : identityType

// AcrPull role definition ID
var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

// Grant ACR pull access to the user-assigned identity
resource containerRegistryAccessUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (usePrivateRegistry && !empty(identityName)) {
  name: guid(containerRegistry.id, userIdentity.id, acrPullRoleId)
  scope: containerRegistry
  properties: {
    principalId: userIdentity.properties.principalId
    roleDefinitionId: acrPullRoleId
    principalType: 'ServicePrincipal'
  }
}

resource app 'Microsoft.App/containerApps@2023-05-01' = {
  name: name
  location: location
  tags: tags
  // Ensure ACR pull access is granted before creating the app
  dependsOn: usePrivateRegistry && !empty(identityName) ? [ containerRegistryAccessUser ] : []
  identity: {
    type: normalizedIdentityType
    userAssignedIdentities: !empty(identityName) && normalizedIdentityType == 'UserAssigned' ? { '${userIdentity.id}': {} } : null
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: revisionMode
      ingress: ingressEnabled ? {
        external: external
        targetPort: targetPort
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: union([ 'https://portal.azure.com', 'https://ms.portal.azure.com' ], allowedOrigins)
        }
      } : null
      registries: usePrivateRegistry ? [
        {
          server: '${containerRegistryName}.azurecr.io'
          identity: !empty(identityName) ? userIdentity.id : null
        }
      ] : []
    }
    template: {
      containers: [
        {
          image: !empty(imageName) ? imageName : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: containerName
          env: env
          resources: {
            cpu: json(containerCpuCoreCount)
            memory: containerMemory
          }
        }
      ]
      scale: {
        minReplicas: containerMinReplicas
        maxReplicas: containerMaxReplicas
      }
    }
  }
}

// Grant ACR pull access to system-assigned identity (must be done after app is created)
resource containerRegistryAccessSystem 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (usePrivateRegistry && normalizedIdentityType == 'SystemAssigned') {
  name: guid(containerRegistry.id, app.id, acrPullRoleId)
  scope: containerRegistry
  properties: {
    principalId: app.identity.principalId
    roleDefinitionId: acrPullRoleId
    principalType: 'ServicePrincipal'
  }
}

output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
output identityPrincipalId string = normalizedIdentityType == 'SystemAssigned' ? app.identity.principalId : ''
output imageName string = imageName
output name string = app.name
output uri string = ingressEnabled ? 'https://${app.properties.configuration.ingress.fqdn}' : ''
output fqdn string = ingressEnabled ? app.properties.configuration.ingress.fqdn : ''
