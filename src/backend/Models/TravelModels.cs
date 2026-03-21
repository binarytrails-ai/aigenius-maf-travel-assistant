using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ContosoTravelAgent.Host.Models;

/// <summary>
/// Structured trip search request extracted from natural language
/// Demonstrates: Importance of strongly-typed structured input
/// </summary>
public record TripSearchRequest
{
    [Description("Departure city or airport code")]
    [JsonPropertyName("origin")]
    public required string Origin { get; init; }
    
    [Description("Destination city, country, or airport code")]
    [JsonPropertyName("destination")]
    public required string Destination { get; init; }
    
    [Description("Departure date in ISO format (YYYY-MM-DD)")]
    [JsonPropertyName("departureDate")]
    public required DateOnly DepartureDate { get; init; }
    
    [Description("Return date in ISO format (YYYY-MM-DD)")]
    [JsonPropertyName("returnDate")]
    public required DateOnly ReturnDate { get; init; }
    
    [Description("Number of travelers")]
    [JsonPropertyName("travelers")]
    public int Travelers { get; init; } = 1;
    
    [Description("Maximum budget in AUD")]
    [JsonPropertyName("maxBudget")]
    public decimal? MaxBudget { get; init; }
}

/// <summary>
/// Flight search result with structured data
/// Demonstrates: Strongly-typed output ensures data consistency
/// </summary>
public record FlightOption
{
    [JsonPropertyName("flightNumber")]
    public required string FlightNumber { get; init; }
    
    [JsonPropertyName("airline")]
    public required string Airline { get; init; }
    
    [JsonPropertyName("price")]
    public required decimal Price { get; init; }
    
    [JsonPropertyName("departureTime")]
    public required DateTime DepartureTime { get; init; }
    
    [JsonPropertyName("arrivalTime")]
    public required DateTime ArrivalTime { get; init; }
    
    [JsonPropertyName("flightProfile")]
    public string? FlightProfile { get; init; }
    
    [JsonPropertyName("similarityScore")]
    public double? SimilarityScore { get; set; }
}

/// <summary>
/// Calendar conflict information
/// Demonstrates: Structured output for complex validation logic
/// </summary>
public record CalendarConflict
{
    [JsonPropertyName("eventName")]
    public required string EventName { get; init; }
    
    [JsonPropertyName("eventDate")]
    public required DateOnly EventDate { get; init; }
    
    [JsonPropertyName("conflictType")]
    public required string ConflictType { get; init; }  // "meeting", "deadline", "event"
    
    [JsonPropertyName("canReschedule")]
    public bool CanReschedule { get; init; }
    
    [JsonPropertyName("priority")]
    public string Priority { get; init; } = "medium";  // "high", "medium", "low"
}
