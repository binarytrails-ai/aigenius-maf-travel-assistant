# Define the .env file path
$envFilePath = ".env"

function Get-AzdValueSafe {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Key
	)

	$value = azd env get-value $Key 2>$null
	if ($LASTEXITCODE -ne 0) {
		return ""
	}

	if ($null -eq $value) {
		return ""
	}

	$text = [string]$value
	if ($text.TrimStart().StartsWith("ERROR:", [System.StringComparison]::OrdinalIgnoreCase)) {
		return ""
	}

	return $text
}

# Clear the contents of the .env file
Set-Content -Path $envFilePath -Value ""

# Append new values to the .env file
$azureEnvName = Get-AzdValueSafe "AZURE_ENV_NAME"
$azureLocation = Get-AzdValueSafe "AZURE_LOCATION"
$azureAIProjectEndpoint = Get-AzdValueSafe "AZURE_AI_PROJECT_ENDPOINT"
$azureAIFoundryServiceEndpoint = Get-AzdValueSafe "AZURE_AI_FOUNDRY_SERVICE_ENDPOINT"
$azureResourceGroup = Get-AzdValueSafe "AZURE_RESOURCE_GROUP"
$azureSubscriptionId = Get-AzdValueSafe "AZURE_SUBSCRIPTION_ID"
$azureTenantId = Get-AzdValueSafe "AZURE_TENANT_ID"
$openAIDeploymentName = Get-AzdValueSafe "AZURE_OPENAI_DEPLOYMENT_NAME"
$embeddingModelName = Get-AzdValueSafe "AZURE_EMBEDDING_MODEL_NAME"
$cosmosDbEndpoint = Get-AzdValueSafe "COSMOS_DB_ENDPOINT"
$cosmosDbConnectionString = Get-AzdValueSafe "COSMOS_DB_CONNECTION_STRING"
$cosmosDbDatabaseName = Get-AzdValueSafe "COSMOS_DB_DATABASE_NAME"
$cosmosDbChatHistoryContainer = Get-AzdValueSafe "COSMOS_DB_CHAT_HISTORY_CONTAINER"
$azureAIServicesEndpoint = Get-AzdValueSafe "AZURE_AI_SERVICES_ENDPOINT"
$azureAIServicesKey = Get-AzdValueSafe "AZURE_AI_SERVICES_KEY"
$mcpFlightSearchToolBaseUrl = Get-AzdValueSafe "MCP_FLIGHT_SEARCH_TOOL_BASE_URL"
$mcpFlightSearchApiKey = "F3FF9AB9-AF9E-42CA-916F-23BEFE7AA546"
$applicationInsightsConnectionString = Get-AzdValueSafe "AZURE_APP_INSIGHTS_CONNECTION_STRING"
$backendUri = Get-AzdValueSafe "BACKEND_URI"
$frontendUri = Get-AzdValueSafe "FRONTEND_URI"

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
Add-Content -Path $envFilePath -Value "MCP_FLIGHT_SEARCH_TOOL_BASE_URL=$mcpFlightSearchToolBaseUrl"
Add-Content -Path $envFilePath -Value "MCP_FLIGHT_SEARCH_API_KEY=$mcpFlightSearchApiKey"
Add-Content -Path $envFilePath -Value "BACKEND_URI=$backendUri"
Add-Content -Path $envFilePath -Value "FRONTEND_URI=$frontendUri"
Add-Content -Path $envFilePath -Value "APPLICATIONINSIGHTS_CONNECTION_STRING=$applicationInsightsConnectionString"

# Write-Host "[INFO] Please visit web app URL:"
# Write-Host $serviceAPIUri -ForegroundColor Cyan