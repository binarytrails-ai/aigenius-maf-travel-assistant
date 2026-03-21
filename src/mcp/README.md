# Contoso Travel MCP Server

An MCP (Model Context Protocol) server that provides flight search capabilities using Azure Cosmos DB.

## Features

- **SearchFlights**: Search for available flights between two cities with optional semantic matching based on user preferences
- **GetFlightByNumber**: Get detailed flight information by flight number

## Prerequisites

- .NET 10.0 SDK
- Azure Cosmos DB account with flight data
- (Optional) GitHub Models or Azure OpenAI for semantic search

## Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `COSMOS_DB_CONNECTION_STRING` | Cosmos DB connection string | Yes (or use endpoint) |
| `COSMOS_DB_ENDPOINT` | Cosmos DB endpoint URL | Yes (or use connection string) |
| `COSMOS_DB_DATABASE_NAME` | Database name (default: ContosoTravel) | No |
| `COSMOS_DB_FLIGHTS_CONTAINER` | Flights container name (default: Flights) | No |
| `USE_GITHUB_MODELS` | Use GitHub Models for embeddings | No |
| `GITHUB_TOKEN` | GitHub token for GitHub Models | Required if USE_GITHUB_MODELS=true |
| `AZURE_AI_SERVICES_ENDPOINT` | Azure AI Services endpoint | Required if not using GitHub Models |
| `MCP_FLIGHT_SEARCH_API_KEY` | API key for authentication | Yes (or configure in appsettings.json) |

## Authentication

The MCP server uses API Key authentication to secure all endpoints. Clients must include a valid API key in the request header.

### Configuration

Configure the API key in one of three ways:

1. **appsettings.json** (production):
   ```json
   {
     "ApiKey": {
       "Value": "your-secure-api-key-here",
       "HeaderName": "X-API-KEY"
     }
   }
   ```

2. **appsettings.Development.json** (local development):
   ```json
   {
     "ApiKey": {
       "Value": "F3FF9AB9-AF9E-42CA-916F-23BEFE7AA546",
       "HeaderName": "X-API-KEY"
     }
   }
   ```

3. **Environment Variable**:
   ```bash
   export MCP_FLIGHT_SEARCH_API_KEY="your-secure-api-key"
   ```

### Making Authenticated Requests

All requests to the MCP server must include the API key in the header:

```bash
curl -H "X-API-KEY: your-secure-api-key-here" https://your-server.com/mcp
```

For MCP clients, configure the header in your client configuration:

```json
{
  "mcpServers": {
    "contoso-travel": {
      "url": "https://your-deployed-server.com/mcp",
      "headers": {
        "X-API-KEY": "your-secure-api-key-here"
      }
    }
  }
}
```

## Building

```bash
dotnet build
```

## Running

The MCP server uses stdio transport:

```bash
dotnet run
```

## Usage with MCP Clients

### Local Development (stdio transport)

For local debugging and testing, configure your MCP client to spawn the server as a subprocess:

```json
{
  "mcpServers": {
    "contoso-travel": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/ContosoTravel.McpServer.csproj"]
    }
  }
}
```

### Remote/Deployed Server (HTTP transport)

For connecting to a running server (e.g., deployed to Azure Container Apps):

```json
{
  "mcpServers": {
    "contoso-travel": {
      "url": "https://your-deployed-server.com/mcp"
    }
  }
}
```

## Available Tools

### SearchFlights

Search for flights between two cities with semantic preference matching.

Parameters:
- `origin` (required): Departure city or airport code
- `destination` (required): Destination city or airport code
- `maxBudget` (optional): Maximum budget in AUD
- `userPreferences` (optional): Natural language preferences for semantic matching

### GetFlightByNumber

Get detailed information about a specific flight.

Parameters:
- `flightNumber` (required): Flight number (e.g., 'QF25')