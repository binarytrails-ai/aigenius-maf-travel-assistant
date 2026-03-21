using System.Text.Json;

namespace ContosoTravelAgent.Host.Extensions;

internal sealed class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextMiddleware> _logger;

    public RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/agent"))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Extract threadId from request body
            try
            {
                var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("threadId", out var threadIdProp))
                {
                    var threadId = threadIdProp.GetString();
                    _logger.LogInformation("Extracted ThreadId: {ThreadId}", threadId);
                    // Store threadId in HttpContext.Items for use in agent/middleware
                    context.Items["ThreadId"] = threadId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract threadId from request body");
            }
        }

        await _next(context);
    }
}

// Extension method to register the middleware
public static class RequestContextMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestContextMiddleware>();
    }
}
