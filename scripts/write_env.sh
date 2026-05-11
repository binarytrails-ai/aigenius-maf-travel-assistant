#!/bin/bash

# Define the .env file path
ENV_FILE_PATH=".env"

get_azd_value_safe() {
	local key="$1"
	local value

	value="$(azd env get-value "$key" 2>&1)"
	if [ $? -ne 0 ]; then
		echo ""
		return
	fi

	case "$value" in
		ERROR:*)
			echo ""
			;;
		*)
			echo "$value"
			;;
	esac
}

# Clear the contents of the .env file
> $ENV_FILE_PATH
echo "AZURE_ENV_NAME=$(get_azd_value_safe AZURE_ENV_NAME)" >> $ENV_FILE_PATH
echo "AZURE_LOCATION=$(get_azd_value_safe AZURE_LOCATION)" >> $ENV_FILE_PATH
echo "AZURE_AI_PROJECT_ENDPOINT=$(get_azd_value_safe AZURE_AI_PROJECT_ENDPOINT)" >> $ENV_FILE_PATH
echo "AZURE_AI_FOUNDRY_SERVICE_ENDPOINT=$(get_azd_value_safe AZURE_AI_FOUNDRY_SERVICE_ENDPOINT)" >> $ENV_FILE_PATH
echo "AZURE_RESOURCE_GROUP=$(get_azd_value_safe AZURE_RESOURCE_GROUP)" >> $ENV_FILE_PATH
echo "AZURE_SUBSCRIPTION_ID=$(get_azd_value_safe AZURE_SUBSCRIPTION_ID)" >> $ENV_FILE_PATH
echo "AZURE_TENANT_ID=$(get_azd_value_safe AZURE_TENANT_ID)" >> $ENV_FILE_PATH
echo "AZURE_TEXT_MODEL_NAME=$(get_azd_value_safe AZURE_OPENAI_DEPLOYMENT_NAME)" >> $ENV_FILE_PATH
echo "AZURE_EMBEDDING_MODEL_NAME=$(get_azd_value_safe AZURE_EMBEDDING_MODEL_NAME)" >> $ENV_FILE_PATH
echo "COSMOS_DB_ENDPOINT=$(get_azd_value_safe COSMOS_DB_ENDPOINT)" >> $ENV_FILE_PATH
echo "COSMOS_DB_CONNECTION_STRING=$(get_azd_value_safe COSMOS_DB_CONNECTION_STRING)" >> $ENV_FILE_PATH
echo "COSMOS_DB_DATABASE_NAME=$(get_azd_value_safe COSMOS_DB_DATABASE_NAME)" >> $ENV_FILE_PATH
echo "COSMOS_DB_CHAT_HISTORY_CONTAINER=$(get_azd_value_safe COSMOS_DB_CHAT_HISTORY_CONTAINER)" >> $ENV_FILE_PATH
echo "AZURE_AI_SERVICES_ENDPOINT=$(get_azd_value_safe AZURE_AI_SERVICES_ENDPOINT)" >> $ENV_FILE_PATH
echo "AZURE_AI_SERVICES_KEY=$(get_azd_value_safe AZURE_AI_SERVICES_KEY)" >> $ENV_FILE_PATH
echo "MCP_FLIGHT_SEARCH_TOOL_BASE_URL=$(get_azd_value_safe MCP_FLIGHT_SEARCH_TOOL_BASE_URL)" >> $ENV_FILE_PATH
echo "MCP_FLIGHT_SEARCH_API_KEY=F3FF9AB9-AF9E-42CA-916F-23BEFE7AA546" >> $ENV_FILE_PATH
echo "BACKEND_URL=$(get_azd_value_safe BACKEND_URI)" >> $ENV_FILE_PATH
echo "FRONTEND_URL=$(get_azd_value_safe FRONTEND_URI)" >> $ENV_FILE_PATH
echo "APPLICATIONINSIGHTS_CONNECTION_STRING=$(get_azd_value_safe AZURE_APP_INSIGHTS_CONNECTION_STRING)" >> $ENV_FILE_PATH

exit 0