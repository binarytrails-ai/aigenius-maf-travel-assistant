namespace ContosoTravelAgent.Host.Models;

public class UserProfileMemory
{
    // "budget backpacker", "luxury", "family", "adventure", "cultural"
    public string? TravelStyle { get; set; }

    // "$1000-2000", "$3000+", "budget-friendly"
    public string? BudgetRange { get; set; }

    // ["hiking", "beaches", "museums"] - keep top 3-5
    public List<string>? Interests { get; set; } 

    public List<PastTrip>? PastDestinations { get; set; }

    // Number of people traveling (e.g., 2, 4)
    public int? NumberOfTravelers { get; set; }

    // "weekend", "1 week", "2 weeks", "1 month+"
    public string? TripDuration { get; set; }

    // "vegetarian", "vegan", "gluten-free", "halal", "kosher", "none"
    public string? DietaryRequirements { get; set; }
}

public class PastTrip
{
    public string? Destination { get; set; }
    public string? Rating { get; set; } // "loved it", "okay", "disappointing"
}
