using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ContosoTravel.McpServer.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration) 
        : base(options, logger, encoder)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get API key from configuration (check both ApiKey:Value and ApiKey)
        var apiKey = _configuration["ApiKey:Value"] ?? 
                    _configuration["ApiKey"] ?? 
                    Environment.GetEnvironmentVariable("MCP_FLIGHT_SEARCH_API_KEY");
        
        // Get API key from the request header
        if (!Request.Headers.TryGetValue(Options.ApiKeyHeaderName, out var extractedApiKey))
        {
            Logger.LogWarning("API key was not provided in request header: {HeaderName}", Options.ApiKeyHeaderName);
            return Task.FromResult(AuthenticateResult.Fail($"API key not found in request header: {Options.ApiKeyHeaderName}"));
        }

        // Validate the API key with constant-time comparison to prevent timing attacks
        if (!string.Equals(apiKey, extractedApiKey, StringComparison.Ordinal))
        {
            Logger.LogWarning("Invalid API key was provided.");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        // API key is valid, create authenticated principal with claims
        var claims = new[]
        { 
            new Claim(ClaimTypes.Name, Options.AuthenticatedUserName),
            new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
            new Claim(ClaimTypes.NameIdentifier, Options.AuthenticatedUserName)
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principalResult = new ClaimsPrincipal(identity);
        var ticketResult = new AuthenticationTicket(principalResult, Scheme.Name);
        
        return Task.FromResult(AuthenticateResult.Success(ticketResult));
    }
}
