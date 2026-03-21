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