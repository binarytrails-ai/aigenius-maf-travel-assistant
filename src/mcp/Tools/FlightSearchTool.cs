using System.ComponentModel;
using System.Text.Json;
using ContosoTravel.McpServer.Models;
using Microsoft.Azure.Cosmos;
using ModelContextProtocol.Server;
using OpenAI.Embeddings;

namespace ContosoTravel.McpServer.Tools;

[McpServerToolType]
public sealed class FlightSearchTool
{
    private readonly Database _database;
    private readonly AppConfig _config;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<FlightSearchTool> _logger;
    private bool semanticSearchEnabled = false;

    public FlightSearchTool(
        Database database,
        AppConfig config,
        EmbeddingClient embeddingClient,
        ILogger<FlightSearchTool> logger)
    {
        _database = database;
        _config = config;
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    [McpServerTool, Description("Search for available flights between two cities. Returns flight options with prices, times, and airline details. Supports semantic matching based on user preferences.")]
    public async Task<string> SearchFlights(
        [Description("Departure city or airport code (e.g., 'Melbourne', 'MEL')")] string origin,
        [Description("Destination city or airport code (e.g., 'Tokyo', 'NRT', 'Auckland', 'AKL')")] string destination,
        [Description("Maximum budget in AUD (optional)")] decimal? maxBudget = null,
        [Description("User preferences for flight characteristics (e.g., 'comfortable flight with entertainment', 'budget-friendly', 'business travel') (optional)")] string? userPreferences = null)
    {
        try
        {
            _logger.LogInformation("[FlightSearch] Searching flights from {Origin} to {Destination}", origin, destination);

            var container = _database.GetContainer(_config.CosmosDbFlightsContainer);

            // Generate embedding for user preferences if provided
            float[]? preferenceVector = null;
            if (semanticSearchEnabled && !string.IsNullOrEmpty(userPreferences))
            {
                _logger.LogInformation("[FlightSearch] Generating embedding for preferences: {Preferences}", userPreferences);
                var embeddingResponse = await _embeddingClient.GenerateEmbeddingAsync(userPreferences);
                preferenceVector = embeddingResponse.Value.ToFloats().ToArray();
            }

            // Build query with native vector search if preferences provided
            var flights = new List<FlightOption>();

            if (preferenceVector != null)
            {
                var queryText = @"SELECT c, VectorDistance(c.flightProfileVector, @preferenceVector) AS SimilarityScore
                    FROM c 
                    WHERE c.type = 'flight' 
                    AND (CONTAINS(UPPER(c.route.origin.city), @origin) OR CONTAINS(UPPER(c.route.origin.code), @origin))
                    AND (CONTAINS(UPPER(c.route.destination.city), @destination) OR CONTAINS(UPPER(c.route.destination.code), @destination))
                    ORDER BY VectorDistance(c.flightProfileVector, @preferenceVector)";

                var queryDefinition = new QueryDefinition(queryText)
                    .WithParameter("@origin", origin.ToUpper())
                    .WithParameter("@destination", destination.ToUpper())
                    .WithParameter("@preferenceVector", preferenceVector);

                using var iterator = container.GetItemQueryIterator<FlightQueryResult>(queryDefinition);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var queryResult in response)
                    {
                        if (queryResult.Flight != null)
                        {
                            var flight = MapToFlightOption(queryResult.Flight);
                            if (queryResult.SimilarityScore.HasValue)
                            {
                                flight.SimilarityScore = 1.0 - queryResult.SimilarityScore.Value;
                            }
                            flights.Add(flight);
                        }
                    }
                }
            }
            else
            {
                var queryText = @"SELECT * FROM c 
                    WHERE c.type = 'flight' 
                    AND (CONTAINS(UPPER(c.route.origin.city), @origin) OR CONTAINS(UPPER(c.route.origin.code), @origin))
                    AND (CONTAINS(UPPER(c.route.destination.city), @destination) OR CONTAINS(UPPER(c.route.destination.code), @destination))";

                var queryDefinition = new QueryDefinition(queryText)
                    .WithParameter("@origin", origin.ToUpper())
                    .WithParameter("@destination", destination.ToUpper());

                using var iterator = container.GetItemQueryIterator<FlightDocument>(queryDefinition);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        flights.Add(MapToFlightOption(doc));
                    }
                }
            }

            // Filter by budget if provided
            var filteredFlights = maxBudget.HasValue
                ? flights.Where(f => f.Price <= maxBudget.Value).ToList()
                : flights;

            // Results are already ordered by vector search if preferences provided, otherwise order by price
            var orderedFlights = !string.IsNullOrEmpty(userPreferences)
                ? filteredFlights
                : filteredFlights.OrderBy(f => f.Price).ToList();

            _logger.LogInformation("[FlightSearch] Found {Count} flights", orderedFlights.Count);

            var result = new
            {
                success = true,
                searchCriteria = new
                {
                    origin,
                    destination,
                    maxBudget = maxBudget?.ToString("C") ?? "No limit",
                    userPreferences = userPreferences ?? "None",
                    semanticSearchEnabled = !string.IsNullOrEmpty(userPreferences)
                },
                totalResults = orderedFlights.Count,
                flights = orderedFlights
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "[FlightSearch] Cosmos DB error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new { error = $"Database error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FlightSearch] Unexpected error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new { error = $"Internal error: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get flight details by flight number.")]
    public async Task<string> GetFlightByNumber(
        [Description("Flight number (e.g., 'QF25', 'VA55')")] string flightNumber)
    {
        try
        {
            _logger.LogInformation("[FlightSearch] Getting flight {FlightNumber}", flightNumber);

            var container = _database.GetContainer(_config.CosmosDbFlightsContainer);

            var queryText = @"SELECT * FROM c 
                WHERE c.type = 'flight' 
                AND UPPER(c.flightNumber) = @flightNumber";

            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@flightNumber", flightNumber.ToUpper());

            using var iterator = container.GetItemQueryIterator<FlightDocument>(queryDefinition);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var doc = response.FirstOrDefault();

                if (doc != null)
                {
                    var result = new
                    {
                        success = true,
                        flight = new
                        {
                            flightNumber = doc.FlightNumber,
                            airline = new
                            {
                                name = doc.Airline?.Name,
                                code = doc.Airline?.Code
                            },
                            route = new
                            {
                                origin = new
                                {
                                    city = doc.Route?.Origin?.City,
                                    code = doc.Route?.Origin?.Code,
                                    country = doc.Route?.Origin?.Country
                                },
                                destination = new
                                {
                                    city = doc.Route?.Destination?.City,
                                    code = doc.Route?.Destination?.Code,
                                    country = doc.Route?.Destination?.Country
                                }
                            },
                            schedule = new
                            {
                                departure = doc.Schedule?.Departure,
                                arrival = doc.Schedule?.Arrival,
                                duration = $"{doc.Schedule?.DurationMinutes / 60}h {doc.Schedule?.DurationMinutes % 60}m"
                            },
                            pricing = new
                            {
                                amount = doc.Pricing?.Amount,
                                currency = doc.Pricing?.Currency
                            },
                            stops = doc.Stops,
                            amenities = doc.Amenities ?? new List<string>(),
                            flightProfile = doc.FlightProfile
                        }
                    };

                    return JsonSerializer.Serialize(result, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
            }

            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Flight {flightNumber} not found"
            });
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "[FlightSearch] Cosmos DB error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new { error = $"Database error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FlightSearch] Unexpected error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new { error = $"Internal error: {ex.Message}" });
        }
    }

    private static FlightOption MapToFlightOption(FlightDocument doc)
    {
        return new FlightOption
        {
            FlightNumber = doc.FlightNumber ?? string.Empty,
            Airline = doc.Airline?.Name ?? string.Empty,
            Price = doc.Pricing?.Amount ?? 0,
            DepartureTime = DateTime.Parse($"2026-01-31T{doc.Schedule?.Departure}"),
            ArrivalTime = DateTime.Parse($"2026-01-31T{doc.Schedule?.Arrival}"),
            Origin = $"{doc.Route?.Origin?.City} ({doc.Route?.Origin?.Code})",
            Destination = $"{doc.Route?.Destination?.City} ({doc.Route?.Destination?.Code})",
            Duration = $"{doc.Schedule?.DurationMinutes / 60}h {doc.Schedule?.DurationMinutes % 60}m",
            Stops = doc.Stops
        };
    }
}
