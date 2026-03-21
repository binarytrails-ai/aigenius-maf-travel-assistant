namespace ContosoTravelAgent.Host.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents user profile and travel preferences for the Contoso Travel Agent.
/// </summary>
public class UserProfileMemory
{
    /// <summary>
    /// Unique identifier for the profile document (Cosmos DB requirement).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID - used as partition key in Cosmos DB.
    /// </summary>
    [JsonPropertyName("UserId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Optional Application ID to scope the profile to a specific application.
    /// </summary>
    [JsonPropertyName("ApplicationId")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Optional Agent ID to scope the profile to a specific agent.
    /// </summary>
    [JsonPropertyName("AgentId")]
    public string? AgentId { get; set; }

    /// <summary>
    /// The user's travel style preference (e.g., "budget backpacker", "luxury", "family", "adventure", "cultural").
    /// </summary>
    public string? TravelStyle { get; set; }

    /// <summary>
    /// The user's budget range for travel (e.g., "$1000-2000", "$3000+", "budget-friendly").
    /// </summary>
    public string? BudgetRange { get; set; }

    /// <summary>
    /// List of user interests (e.g., "hiking", "beaches", "museums"). Keep top 3-5.
    /// </summary>
    public List<string>? Interests { get; set; } 

    /// <summary>
    /// List of past trips and destinations the user has visited.
    /// </summary>
    public List<PastTrip>? PastDestinations { get; set; }

    /// <summary>
    /// Number of people traveling (e.g., 2, 4).
    /// </summary>
    public int? NumberOfTravelers { get; set; }

    /// <summary>
    /// Typical trip duration preference (e.g., "weekend", "1 week", "2 weeks", "1 month+").
    /// </summary>
    public string? TripDuration { get; set; }

    /// <summary>
    /// Dietary requirements (e.g., "vegetarian", "vegan", "gluten-free", "halal", "kosher", "none").
    /// </summary>
    public string? DietaryRequirements { get; set; }
}

/// <summary>
/// Represents a past trip taken by the user.
/// </summary>
public class PastTrip
{
    /// <summary>
    /// The destination of the past trip.
    /// </summary>
    public string? Destination { get; set; }
    
    /// <summary>
    /// User's rating of the trip (e.g., "loved it", "okay", "disappointing").
    /// </summary>
    public string? Rating { get; set; }
}
