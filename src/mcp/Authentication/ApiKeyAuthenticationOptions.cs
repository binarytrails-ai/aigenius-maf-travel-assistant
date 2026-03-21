using Microsoft.AspNetCore.Authentication;

namespace ContosoTravel.McpServer.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// The default authentication scheme name for API key authentication
    /// </summary>
    public const string DefaultScheme = "ApiKeyScheme";
    
    /// <summary>
    /// The HTTP header name where the API key is expected
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-API-KEY";
    
    /// <summary>
    /// Whether to allow requests when no API key is configured
    /// This is useful for development environments
    /// </summary>
    public bool AllowRequestsWithoutApiKeyConfiguration { get; set; } = true;
    
    /// <summary>
    /// The name to use for the authenticated user when API key is valid
    /// </summary>
    public string AuthenticatedUserName { get; set; } = "ApiKeyUser";
}
