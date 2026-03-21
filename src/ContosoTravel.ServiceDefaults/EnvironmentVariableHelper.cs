using DotNetEnv;

namespace ContosoTravel.ServiceDefaults;

/// <summary>
/// Shared helper for loading environment variables from .env files.
/// Used by both Aspire AppHost and individual projects for consistent behavior.
/// </summary>
public static class EnvironmentVariableHelper
{
    private static bool _loaded = false;
    private static readonly object _lock = new();

    /// <summary>
    /// Loads the .env file from the workspace root by searching up the directory tree.
    /// This method is idempotent - calling it multiple times has no additional effect.
    /// </summary>
    /// <param name="startDirectory">Optional starting directory. Defaults to current directory.</param>
    /// <returns>True if a .env file was found and loaded; false otherwise.</returns>
    public static bool LoadEnvironmentVariables(string? startDirectory = null)
    {
        lock (_lock)
        {
            if (_loaded)
            {
                return true;
            }

            var fileName = ".env";
            var currentDir = new DirectoryInfo(startDirectory ?? Directory.GetCurrentDirectory());

            while (currentDir != null)
            {
                var envFile = Path.Combine(currentDir.FullName, fileName);
                if (File.Exists(envFile))
                {
                    Env.Load(envFile);
                    Console.WriteLine($"Loaded .env from: {envFile}");
                    _loaded = true;
                    return true;
                }
                currentDir = currentDir.Parent;
            }

            Console.WriteLine("No .env file found");
            return false;
        }
    }

    /// <summary>
    /// Gets an environment variable value with fallback to IConfiguration.
    /// Priority: Environment variable > .env file > IConfiguration > default value
    /// </summary>
    /// <param name="key">The environment variable key.</param>
    /// <param name="configuration">Optional IConfiguration instance for fallback.</param>
    /// <param name="defaultValue">Optional default value if not found.</param>
    /// <returns>The configuration value or default.</returns>
    public static string? GetConfigValue(
        string key, 
        Microsoft.Extensions.Configuration.IConfiguration? configuration = null, 
        string? defaultValue = null)
    {
        // First try DotNetEnv (which includes system environment variables)
        var value = Env.GetString(key);
        
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Then try IConfiguration
        if (configuration != null)
        {
            value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets a required configuration value. Throws if not found.
    /// </summary>
    /// <param name="key">The environment variable key.</param>
    /// <param name="configuration">Optional IConfiguration instance for fallback.</param>
    /// <returns>The configuration value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is not found.</exception>
    public static string GetRequiredConfigValue(
        string key, 
        Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    {
        var value = GetConfigValue(key, configuration);
        
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required configuration '{key}' not found. Set it in .env file or environment variables.");
        }

        return value;
    }

    /// <summary>
    /// Gets a boolean configuration value with fallback.
    /// </summary>
    public static bool GetBoolConfigValue(
        string key, 
        Microsoft.Extensions.Configuration.IConfiguration? configuration = null, 
        bool defaultValue = false)
    {
        var value = GetConfigValue(key, configuration);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Resets the loaded state. Useful for testing.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _loaded = false;
        }
    }
}
