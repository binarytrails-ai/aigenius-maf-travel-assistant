using ContosoTravel.McpServer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Load configuration and register services
var config = builder.Services.LoadMcpServerConfig(builder.Configuration);
builder.Services.AddCosmosDb(config);
builder.Services.AddEmbeddingClient(config);

// Add MCP server with HTTP transport for deployment
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

// Map MCP endpoint
app.MapMcp("/mcp");

// Health endpoint for container apps
app.MapGet("/health", () => Results.Ok("Healthy"));

// Run the app
await app.RunAsync();
