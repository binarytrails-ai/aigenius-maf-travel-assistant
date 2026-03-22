
var builder = DistributedApplication.CreateBuilder(args);

// MCP Server - Flight Search Tool
var mcpServer = builder.AddProject<Projects.ContosoTravel_McpServer>("mcp-server")
    .WithExternalHttpEndpoints();

// Backend API - Contoso Travel Agent
var backend = builder.AddProject<Projects.ContosoTravelAgent_Host>("backend")
    .WithExternalHttpEndpoints()
    .WithReference(mcpServer)
    .WaitFor(mcpServer)
    .WithEnvironment("MCP_FLIGHT_SEARCH_TOOL_BASE_URL", mcpServer.GetEndpoint("http"));

// Frontend - Next.js application (uses Dockerfile for container deployment)
var frontend = builder.AddDockerfile("frontend", "../frontend")
    .WithHttpEndpoint(3000, targetPort: 3000)
    .WithExternalHttpEndpoints()
    .WithReference(backend)
    .WaitFor(backend)
    .WithEnvironment("BACKEND_AGENT_BASE_URL", backend.GetEndpoint("http"));

await builder.Build().RunAsync();