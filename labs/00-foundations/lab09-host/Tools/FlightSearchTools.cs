using System.ComponentModel;
using System.Text.Json;

namespace Lab05Host.Tools;

/// <summary>
/// Flight search tools for finding available flights
/// </summary>
public static class FlightSearchTools
{
    /// <summary>
    /// Searches for available flights between two cities.
    /// </summary>
    [Description("Search for available flights between two cities. Returns flight options with times, prices, and airline details.")]
    public static async Task<string> SearchFlights(
        [Description("Departure city (e.g., 'Melbourne', 'Sydney')")] string origin,
        [Description("Destination city (e.g., 'Tokyo', 'London')")] string destination,
        [Description("Departure date in YYYY-MM-DD format (optional)")] string? departureDate = null)
    {
        await Task.CompletedTask;

        // Simulate flight search with mock data
        var flights = new[]
        {
            new
            {
                flightNumber = "QF21",
                airline = "Qantas",
                origin,
                destination,
                departure = departureDate ?? DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"),
                departureTime = "09:30",
                arrivalTime = "17:45",
                duration = "10h 15min",
                price = 850,
                currency = "USD",
                cabinClass = "Economy",
                available = true
            },
            new
            {
                flightNumber = "JL772",
                airline = "Japan Airlines",
                origin,
                destination,
                departure = departureDate ?? DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"),
                departureTime = "11:00",
                arrivalTime = "19:20",
                duration = "9h 20min",
                price = 920,
                currency = "USD",
                cabinClass = "Economy",
                available = true
            },
            new
            {
                flightNumber = "ANA880",
                airline = "All Nippon Airways",
                origin,
                destination,
                departure = departureDate ?? DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"),
                departureTime = "14:30",
                arrivalTime = "22:30",
                duration = "9h 00min",
                price = 780,
                currency = "USD",
                cabinClass = "Economy",
                available = true
            }
        };

        var result = new
        {
            success = true,
            searchCriteria = new { origin, destination, departureDate },
            results = flights,
            count = flights.Length,
            message = $"Found {flights.Length} flights from {origin} to {destination}"
        };

        return JsonSerializer.Serialize(result);
    }
}
