# Define the .env file path
$envFilePath = ".env"

# Clear the contents of the .env file
Set-Content -Path $envFilePath -Value ""

# Append new values to the .env file
$azureEnvName = azd env get-value AZURE_ENV_NAME
$azureLocation = azd env get-value AZURE_LOCATION
$azureAIProjectEndpoint = azd env get-value AZURE_AI_PROJECT_ENDPOINT
$azureAIFoundryServiceEndpoint = azd env get-value AZURE_AI_FOUNDRY_SERVICE_ENDPOINT
$azureResourceGroup = azd env get-value AZURE_RESOURCE_GROUP
$azureSubscriptionId = azd env get-value AZURE_SUBSCRIPTION_ID
$azureTenantId = azd env get-value AZURE_TENANT_ID
$openAIDeploymentName = azd env get-value AZURE_OPENAI_DEPLOYMENT_NAME
$embeddingModelName = azd env get-value AZURE_EMBEDDING_MODEL_NAME
$cosmosDbEndpoint = azd env get-value COSMOS_DB_ENDPOINT
$cosmosDbConnectionString = azd env get-value COSMOS_DB_CONNECTION_STRING
$cosmosDbDatabaseName = azd env get-value COSMOS_DB_DATABASE_NAME
$cosmosDbChatHistoryContainer = azd env get-value COSMOS_DB_CHAT_HISTORY_CONTAINER
$azureAIServicesEndpoint = azd env get-value AZURE_AI_SERVICES_ENDPOINT
$azureAIServicesKey = azd env get-value AZURE_AI_SERVICES_KEY

Add-Content -Path $envFilePath -Value "AZURE_ENV_NAME=$azureEnvName"
Add-Content -Path $envFilePath -Value "AZURE_LOCATION=$azureLocation"
Add-Content -Path $envFilePath -Value "AZURE_AI_PROJECT_ENDPOINT=$azureAIProjectEndpoint"
Add-Content -Path $envFilePath -Value "AZURE_AI_FOUNDRY_SERVICE_ENDPOINT=$azureAIFoundryServiceEndpoint"
Add-Content -Path $envFilePath -Value "AZURE_RESOURCE_GROUP=$azureResourceGroup"
Add-Content -Path $envFilePath -Value "AZURE_SUBSCRIPTION_ID=$azureSubscriptionId"
Add-Content -Path $envFilePath -Value "AZURE_TENANT_ID=$azureTenantId"
Add-Content -Path $envFilePath -Value "AZURE_TEXT_MODEL_NAME=$openAIDeploymentName"
Add-Content -Path $envFilePath -Value "AZURE_EMBEDDING_MODEL_NAME=$embeddingModelName"
Add-Content -Path $envFilePath -Value "COSMOS_DB_ENDPOINT=$cosmosDbEndpoint"
Add-Content -Path $envFilePath -Value "COSMOS_DB_CONNECTION_STRING=$cosmosDbConnectionString"
Add-Content -Path $envFilePath -Value "COSMOS_DB_DATABASE_NAME=$cosmosDbDatabaseName"
Add-Content -Path $envFilePath -Value "COSMOS_DB_CHAT_HISTORY_CONTAINER=$cosmosDbChatHistoryContainer"
Add-Content -Path $envFilePath -Value "AZURE_AI_SERVICES_ENDPOINT=$azureAIServicesEndpoint"
Add-Content -Path $envFilePath -Value "AZURE_AI_SERVICES_KEY=$azureAIServicesKey"

# Write-Host "[INFO] Please visit web app URL:"
# Write-Host $serviceAPIUri -ForegroundColor Cyan