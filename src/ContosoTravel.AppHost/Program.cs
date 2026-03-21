
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

// Frontend - Next.js application
var frontend = builder.AddNodeApp("frontend", "../frontend", "start")
    .WithHttpEndpoint(3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(backend)
    .WaitFor(backend)
    .WithEnvironment("BACKEND_AGENT_BASE_URL", backend.GetEndpoint("http"));

await builder.Build().RunAsync();