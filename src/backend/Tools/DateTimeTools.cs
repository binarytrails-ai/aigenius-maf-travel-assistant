using System.ComponentModel;
using System.Text.Json;

namespace ContosoTravelAgent.Host.Tools;

/// <summary>
/// Date and time utility tools for travel planning
/// Provides context awareness for relative dates and validation
/// </summary>
public static class DateTimeTools
{
    /// <summary>
    /// Gets the current date and time information.
    /// </summary>
    [Description("Get the current date and time. Use this when user mentions relative dates like 'next month', 'in 2 weeks', 'this summer', or when you need today's date for calculations.")]
    public static async Task<string> GetCurrentDate()
    {
        await Task.CompletedTask;

        // Use Australian Eastern Time
        var aestZone = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, aestZone);
        var today = DateOnly.FromDateTime(now);

        // Determine current season (Southern Hemisphere - Australia)
        var season = today.Month switch
        {
            12 or 1 or 2 => "Summer",
            3 or 4 or 5 => "Autumn",
            6 or 7 or 8 => "Winter",
            9 or 10 or 11 => "Spring",
            _ => "Unknown"
        };

        var result = new
        {
            success = true,
            currentDateTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
            date = today.ToString("yyyy-MM-dd"),
            dayOfWeek = now.DayOfWeek.ToString(),
            season = season,
            year = now.Year,
            month = now.ToString("MMMM"),
            monthNumber = now.Month,
            day = now.Day,
            timeZone = now.IsDaylightSavingTime() ? "AEDT (UTC+11)" : "AEST (UTC+10)",
            location = "Melbourne, Australia",
            helpfulContext = new
            {
                nextWeek = today.AddDays(7).ToString("yyyy-MM-dd"),
                nextMonth = today.AddMonths(1).ToString("yyyy-MM-dd"),
                threeMonthsOut = today.AddMonths(3).ToString("yyyy-MM-dd")
            }
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Calculates the number of days between two dates.
    /// </summary>
    [Description("Calculate the number of days between two dates. Useful for determining trip duration, booking windows, and date validations.")]
    public static async Task<string> CalculateDateDifference(
        [Description("Start date in YYYY-MM-DD format")] string startDate,
        [Description("End date in YYYY-MM-DD format")] string endDate)
    {
        await Task.CompletedTask;

        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);
        var daysDifference = end.DayNumber - start.DayNumber;

        var result = new
        {
            success = true,
            startDate,
            endDate,
            totalDays = daysDifference,
            totalWeeks = Math.Round(daysDifference / 7.0, 1),
            businessDays = CalculateBusinessDays(start, end),
            isValidRange = daysDifference >= 0,
            message = daysDifference < 0
                ? "End date is before start date!"
                : daysDifference == 0
                    ? "Same-day trip (0 nights)"
                    : $"{daysDifference} day trip ({daysDifference - 1} nights)"
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Validates if given dates are suitable for booking.
    /// </summary>
    [Description("Validate if travel dates are bookable. Checks booking windows, blackout dates, and provides recommendations.")]
    public static async Task<string> ValidateTravelDates(
        [Description("Departure date in YYYY-MM-DD format")] string departureDate,
        [Description("Return date in YYYY-MM-DD format")] string returnDate)
    {
        await Task.CompletedTask;

        var aestZone = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, aestZone);
        var today = DateOnly.FromDateTime(now);
        var departure = DateOnly.Parse(departureDate);

        var returnDate_parsed = departure.AddDays(7);
        if (!string.IsNullOrEmpty(returnDate))
        {
            returnDate_parsed = DateOnly.Parse(returnDate);
        }

        var daysUntilDeparture = departure.DayNumber - today.DayNumber;
        var tripDuration = returnDate_parsed.DayNumber - departure.DayNumber;

        var warnings = new List<string>();
        var recommendations = new List<string>();

        // Validation logic
        if (daysUntilDeparture < 0)
        {
            warnings.Add("Departure date is in the past!");
        }
        else if (daysUntilDeparture < 7)
        {
            warnings.Add("Last-minute booking (<7 days). Expect higher prices and limited availability.");
            recommendations.Add("Consider flexible dates for better deals");
        }
        else if (daysUntilDeparture < 21)
        {
            recommendations.Add("Booking within 3 weeks - good time to lock in prices");
        }
        else if (daysUntilDeparture > 330)
        {
            warnings.Add("Booking very far in advance (>11 months). Flight schedules may change.");
            recommendations.Add("Consider setting a price alert instead of booking now");
        }

        if (tripDuration < 0)
        {
            warnings.Add("Return date is before departure date!");
        }
        else if (tripDuration == 0)
        {
            recommendations.Add("Same-day return - very short trip!");
        }
        else if (tripDuration > 21)
        {
            recommendations.Add("Extended trip (>3 weeks) - check visa requirements");
        }

        // Check if dates fall on major Australian holidays (mock data)
        var holidayDates = new[]
        {
            new DateOnly(2026, 1, 1),   // New Year's Day
            new DateOnly(2026, 1, 26),  // Australia Day
            new DateOnly(2026, 4, 25),  // ANZAC Day
            new DateOnly(2026, 12, 25), // Christmas Day
            new DateOnly(2026, 12, 26)  // Boxing Day
        };

        if (holidayDates.Contains(departure) || holidayDates.Contains(returnDate_parsed))
        {
            warnings.Add("Travel dates include major holidays - expect higher prices and crowds");
        }

        var isValid = !warnings.Any(w => w.Contains("past") || w.Contains("before"));

        var result = new
        {
            success = true,
            isValid,
            dates = new
            {
                departure = departureDate,
                returnDate = returnDate,
                daysUntilDeparture,
                tripDuration
            },
            validation = new
            {
                warnings = warnings,
                recommendations = recommendations
            },
            bookingWindow = daysUntilDeparture switch
            {
                < 7 => "Last Minute",
                < 21 => "Short Notice",
                < 90 => "Standard",
                < 180 => "Advance",
                _ => "Very Early"
            }
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    // Helper method for business days calculation
    private static int CalculateBusinessDays(DateOnly start, DateOnly end)
    {
        var businessDays = 0;
        var current = start;

        while (current <= end)
        {
            var dayOfWeek = current.DayOfWeek;
            if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
            {
                businessDays++;
            }
            current = current.AddDays(1);
        }

        return businessDays;
    }
}
