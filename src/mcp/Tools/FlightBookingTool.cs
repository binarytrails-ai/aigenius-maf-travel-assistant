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

    [McpServerTool, Description("Books a flight based on the flight number, travel date, and passenger details.")]
    public async Task<FlightBookResponse> BookFlight(
        [Description("The flight number to book (e.g., AA123)")] string flightNumber,
        [Description("The date of travel")] DateTime travelDate,
        [Description("Passenger's first name")] string firstName,
        [Description("Passenger's last name")] string lastName,
        [Description("Passenger's passport number")] string passportNumber)
    {
        _logger.LogInformation("Received flight booking request for flight {FlightNumber} on {TravelDate} for passenger {FirstName} {LastName}", 
            flightNumber, travelDate, firstName, lastName);

        // Validate input
        if (string.IsNullOrEmpty(flightNumber))
        {
            _logger.LogWarning("Invalid flight booking request: Flight number is required");
            throw new ArgumentException("Flight number is required.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            _logger.LogWarning("Invalid flight booking request: First name is required");
            throw new ArgumentException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            _logger.LogWarning("Invalid flight booking request: Last name is required");
            throw new ArgumentException("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(passportNumber))
        {
            _logger.LogWarning("Invalid flight booking request: Passport number is required");
            throw new ArgumentException("Passport number is required.");
        }

        // Generate mock booking response
        var bookingId = Guid.NewGuid().ToString();
        var confirmationCode = $"{flightNumber.ToUpper()}-{DateTime.UtcNow.Ticks % 1000000:D6}";
        var totalPrice = new Random().Next(200, 800);

        var result = new FlightBookResponse
        {
            BookingId = bookingId,
            FlightNumber = flightNumber,
            TravelDate = travelDate,
            FirstName = firstName,
            LastName = lastName,
            PassportNumber = passportNumber,
            Status = "Booked",
            ConfirmationCode = confirmationCode,
            TotalPrice = totalPrice
        };

        _logger.LogInformation("Flight booked successfully: {Result}", JsonSerializer.Serialize(result));
        
        await Task.CompletedTask; // Keep async signature for potential future database operations
        
        return result;
    }
}
