using ContosoTravel.McpServer.Authentication;
using ContosoTravel.McpServer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load configuration and register services
var config = builder.Services.LoadMcpServerConfig(builder.Configuration);

// Configure logging and OpenTelemetry
builder.ConfigureOpenTelemetry(
    serviceName: "ContosoTravel.McpServer",
    serviceVersion: "1.0.0",
    otlpEndpoint: config.OtelExporterOtlpEndpoint,
    applicationInsightsConnectionString: config.ApplicationInsightsConnectionString);

builder.Services.AddCosmosDb(config);
builder.Services.AddEmbeddingClient(config);

// Add MCP server with HTTP transport for deployment
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly();

// Configure API Key Authentication
string apiKeyHeaderName = builder.Configuration["ApiKey:HeaderName"] ?? "X-API-KEY";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
    options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
}).AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
    ApiKeyAuthenticationOptions.DefaultScheme,
    options => { options.ApiKeyHeaderName = apiKeyHeaderName; });

// Add authorization to require authentication for all MCP endpoints
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add(ApiKeyAuthenticationOptions.DefaultScheme);
        policy.RequireAuthenticatedUser();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseAuthentication();
app.UseAuthorization();

// Map MCP endpoint with API key authorization
app.MapMcp("/mcp")
   .RequireAuthorization("ApiKeyPolicy");

// Health endpoint for container apps
app.MapGet("/health", () => Results.Ok("Healthy"));

// Run the app
await app.RunAsync();
