@description('Azure region of the deployment')
param location string

@description('Tags to add to the resources')
param tags object

@description('Cosmos DB account name')
param cosmosDbAccountName string

@description('Cosmos DB database name')
param cosmosDbDatabaseName string = 'ContosoTravelDb'

@description('Cosmos DB container name for chat history')
param chatHistoryContainerName string = 'ChatHistory'

@description('Cosmos DB container name for flights')
param flightsContainerName string = 'Flights'

@description('Cosmos DB container name for bookings')
param bookingsContainerName string = 'Bookings'

@description('Vector embedding dimensions')
param vectorDimensions int = 3072

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosDbAccountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableNoSQLVectorSearch'
      }
    ]
    enableFreeTier: true
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosDbAccount
  name: cosmosDbDatabaseName
  properties: {
    resource: {
      id: cosmosDbDatabaseName
    }
  }
}

resource chatHistoryContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: database
  name: chatHistoryContainerName
  properties: {
    resource: {
      id: chatHistoryContainerName
      partitionKey: {
        paths: [
          '/ApplicationId'
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
        vectorIndexes: [
          {
            path: '/ContentEmbedding'
            type: 'diskANN'
          }
        ]
      }
      vectorEmbeddingPolicy: {
        vectorEmbeddings: [
          {
            path: '/ContentEmbedding'
            dataType: 'float32'
            dimensions: vectorDimensions
            distanceFunction: 'cosine'
          }
        ]
      }
    }
    options: {
      throughput: 400
    }
  }
}

resource flightsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: database
  name: flightsContainerName
  properties: {
    resource: {
      id: flightsContainerName
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
        vectorIndexes: [
          {
            path: '/flightProfileVector'
            type: 'diskANN'
          }
        ]
      }
      vectorEmbeddingPolicy: {
        vectorEmbeddings: [
          {
            path: '/flightProfileVector'
            dataType: 'float32'
            dimensions: vectorDimensions
            distanceFunction: 'cosine'
          }
        ]
      }
    }
    options: {
      throughput: 400
    }
  }
}

output cosmosDbAccountId string = cosmosDbAccount.id
output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
output cosmosDbConnectionString string = cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
output cosmosDbDatabaseName string = cosmosDbDatabaseName
output chatHistoryContainerName string = chatHistoryContainerName
