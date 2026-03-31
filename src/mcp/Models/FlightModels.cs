using System.Text.Json.Serialization;

namespace ContosoTravel.McpServer.Models;

/// <summary>
/// Flight search result with structured data
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
    
    [JsonPropertyName("origin")]
    public string? Origin { get; init; }
    
    [JsonPropertyName("destination")]
    public string? Destination { get; init; }
    
    [JsonPropertyName("duration")]
    public string? Duration { get; init; }
    
    [JsonPropertyName("stops")]
    public int? Stops { get; init; }
    
    [JsonPropertyName("similarityScore")]
    public double? SimilarityScore { get; set; }
}

/// <summary>
/// Airline information
/// </summary>
public record AirlineInfo
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }
    
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }
}

/// <summary>
/// Route information
/// </summary>
public record RouteInfo
{
    [JsonPropertyName("origin")]
    public LocationInfo? Origin { get; init; }
    
    [JsonPropertyName("destination")]
    public LocationInfo? Destination { get; init; }
}

/// <summary>
/// Location information
/// </summary>
public record LocationInfo
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }
    
    [JsonPropertyName("city")]
    public string? City { get; init; }
    
    [JsonPropertyName("country")]
    public string? Country { get; init; }
}

/// <summary>
/// Complete flight document from Cosmos DB
/// </summary>
public record FlightDocument
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("type")]
    public string? Type { get; init; }
    
    [JsonPropertyName("flightNumber")]
    public string? FlightNumber { get; init; }
    
    [JsonPropertyName("airline")]
    public AirlineInfo? Airline { get; init; }
    
    [JsonPropertyName("route")]
    public RouteInfo? Route { get; init; }
    
    [JsonPropertyName("schedule")]
    public ScheduleInfo? Schedule { get; init; }
    
    [JsonPropertyName("pricing")]
    public PricingInfo? Pricing { get; init; }
    
    [JsonPropertyName("aircraft")]
    public AircraftInfo? Aircraft { get; init; }
    
    [JsonPropertyName("stops")]
    public int Stops { get; init; }
    
    [JsonPropertyName("amenities")]
    public List<string>? Amenities { get; init; }
    
    [JsonPropertyName("baggage")]
    public BaggageInfo? Baggage { get; init; }
    
    [JsonPropertyName("availability")]
    public int Availability { get; init; }
    
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }
    
    [JsonPropertyName("flightProfile")]
    public string? FlightProfile { get; init; }
    
    [JsonPropertyName("flightProfileVector")]
    public float[]? FlightProfileVector { get; init; }
}

public record ScheduleInfo
{
    [JsonPropertyName("departure")]
    public string? Departure { get; init; }
    
    [JsonPropertyName("arrival")]
    public string? Arrival { get; init; }
    
    [JsonPropertyName("duration")]
    public string? Duration { get; init; }
    
    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; init; }
}

public record PricingInfo
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }
    
    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
    
    [JsonPropertyName("fareClass")]
    public string? FareClass { get; init; }
    
    [JsonPropertyName("bookingClass")]
    public string? BookingClass { get; init; }
}

public record AircraftInfo
{
    [JsonPropertyName("model")]
    public string? Model { get; init; }
    
    [JsonPropertyName("registration")]
    public string? Registration { get; init; }
}

public record BaggageInfo
{
    [JsonPropertyName("cabin")]
    public string? Cabin { get; init; }
    
    [JsonPropertyName("checked")]
    public string? Checked { get; init; }
}

/// <summary>
/// Query result with similarity score
/// </summary>
public record FlightQueryResult
{
    [JsonPropertyName("c")]
    public FlightDocument? Flight { get; init; }
    
    [JsonPropertyName("SimilarityScore")]
    public double? SimilarityScore { get; init; }
}

/// <summary>
/// Flight booking response
/// </summary>
public record FlightBookResponse
{
    [JsonPropertyName("bookingId")]
    public required string BookingId { get; init; }
    
    [JsonPropertyName("flightNumber")]
    public required string FlightNumber { get; init; }
    
    [JsonPropertyName("travelDate")]
    public required DateTime TravelDate { get; init; }
    
    [JsonPropertyName("firstName")]
    public required string FirstName { get; init; }
    
    [JsonPropertyName("lastName")]
    public required string LastName { get; init; }
    
    [JsonPropertyName("passportNumber")]
    public required string PassportNumber { get; init; }
    
    [JsonPropertyName("status")]
    public required string Status { get; init; }
    
    [JsonPropertyName("confirmationCode")]
    public string? ConfirmationCode { get; init; }
    
    [JsonPropertyName("totalPrice")]
    public decimal? TotalPrice { get; init; }
}
