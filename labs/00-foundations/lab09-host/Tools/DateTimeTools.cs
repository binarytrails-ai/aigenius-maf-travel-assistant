using System.ComponentModel;
using System.Text.Json;

namespace Lab05Host.Tools;

/// <summary>
/// Date and time utility tools for travel planning
/// </summary>
public static class DateTimeTools
{
    /// <summary>
    /// Gets the current date and time information.
    /// </summary>
    [Description("Get the current date and time. Use this when user mentions relative dates like 'next week', 'tomorrow', or when you need today's date for calculations.")]
    public static async Task<string> GetCurrentDate()
    {
        await Task.CompletedTask;

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var result = new
        {
            success = true,
            currentDateTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
            date = today.ToString("yyyy-MM-dd"),
            dayOfWeek = now.DayOfWeek.ToString(),
            year = now.Year,
            month = now.ToString("MMMM"),
            day = now.Day
        };

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Calculates days until a future date.
    /// </summary>
    [Description("Calculate how many days until a future date. Target date must be in YYYY-MM-DD format.")]
    public static async Task<string> CalculateDaysUntil(
        [Description("Target date in YYYY-MM-DD format")] string targetDate)
    {
        await Task.CompletedTask;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var target = DateOnly.Parse(targetDate);
        var days = target.DayNumber - today.DayNumber;

        var result = new
        {
            success = true,
            today = today.ToString("yyyy-MM-dd"),
            targetDate = target.ToString("yyyy-MM-dd"),
            daysUntil = days,
            message = days > 0 ? $"{days} days until {target:MMMM dd, yyyy}" : 
                      days == 0 ? "That's today!" : 
                      "Date is in the past"
        };

        return JsonSerializer.Serialize(result);
    }
}
