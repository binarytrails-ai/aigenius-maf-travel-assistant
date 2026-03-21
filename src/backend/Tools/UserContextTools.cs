using System.ComponentModel;
using System.Text.Json;

namespace ContosoTravelAgent.Host.Tools;

/// <summary>
/// User context and profile tools
/// Provides access to user preferences and default settings
/// </summary>
public static class UserContextTools
{
    /// <summary>
    /// Gets user's default travel preferences and location.
    /// DEMO CONCEPT: Context retrieval - if user says "I want to travel", assume origin from profile
    /// </summary>
    [Description("Retrieves the user's travel profile including home city (default departure location), timezone, name, and airline loyalty program memberships with status levels. Call this when the user mentions travel without specifying their origin city.")]
    public static async Task<string> GetUserContext()
    {
        await Task.Delay(200);

        // Mock user context - in production, retrieve from user profile service
        var result = new
        {
            success = true,
            userProfile = new
            {
                firstName = "John",
                lastName = "Doe",
                email = "john.doe@contoso.com",
                dateOfBirth = "1985-06-15",
                defaultOrigin = "Melbourne",
                timeZone = "Australia/Melbourne (AEDT/AEST)",
                preferredLanguage = "English",
                preferredCurrency = "AUD",
                passportInfo = new
                {
                    country = "Australia",
                    expiryDate = "2028-03-20"
                },
                preferences = new
                {
                    seatPreference = "Window",
                    mealPreference = "Vegetarian",
                    DietaryRequirements = new[] { "Vegetarian" },
                    accessibilityNeeds = "None"
                }
            },
            message = "User's default departure city is Melbourne (MEL)"
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
