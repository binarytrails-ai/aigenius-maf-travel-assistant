using System.ComponentModel;
using System.Text.Json;
using ContosoTravel.McpServer.Models;
using Microsoft.Azure.Cosmos;
using ModelContextProtocol.Server;

namespace ContosoTravel.McpServer.Tools;

[McpServerToolType]
public sealed class FlightBookingTool
{
    private readonly Database _database;
    private readonly AppConfig _config;
    private readonly ILogger<FlightBookingTool> _logger;

    public FlightBookingTool(
        Database database,
        AppConfig config,
        ILogger<FlightBookingTool> logger)
    {
        _database = database;
        _config = config;
        _logger = logger;
    }

    [McpServerTool, Description("Books a flight based on the flight number, travel date, and number of passengers.")]
    public async Task<FlightBookResponse> BookFlightAsync(
        [Description("The flight number to book (e.g., AA123)")] string flightNumber,
        [Description("The date of travel")] DateTime travelDate,
        [Description("The number of passengers (defaults to 1)")] int numberOfPassengers = 1)
    {
        _logger.LogInformation("Received flight booking request for flight {FlightNumber} on {TravelDate} for {Passengers} passenger(s)", 
            flightNumber, travelDate, numberOfPassengers);

        // Validate input
        if (string.IsNullOrEmpty(flightNumber))
        {
            _logger.LogWarning("Invalid flight booking request: Flight number is required");
            throw new ArgumentException("Flight number is required.");
        }

        if (numberOfPassengers < 1)
        {
            _logger.LogWarning("Invalid number of passengers: {Passengers}", numberOfPassengers);
            throw new ArgumentException("Number of passengers must be at least 1.");
        }

        // Generate mock booking response
        var bookingId = Guid.NewGuid().ToString();
        var confirmationCode = $"{flightNumber.ToUpper()}-{DateTime.UtcNow.Ticks % 1000000:D6}";
        var pricePerPassenger = new Random().Next(200, 800);
        var totalPrice = pricePerPassenger * numberOfPassengers;

        var result = new FlightBookResponse
        {
            BookingId = bookingId,
            FlightNumber = flightNumber,
            TravelDate = travelDate,
            NumberOfPassengers = numberOfPassengers,
            Status = "Booked",
            ConfirmationCode = confirmationCode,
            TotalPrice = totalPrice
        };

        _logger.LogInformation("Flight booked successfully: {Result}", JsonSerializer.Serialize(result));
        
        await Task.CompletedTask; // Keep async signature for potential future database operations
        
        return result;
    }
}
