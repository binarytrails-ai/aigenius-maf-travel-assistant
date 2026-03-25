using ContosoTravelAgent.Host.Models;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ContosoTravelAgent.Host.Services;

internal sealed class UserProfileMemoryProvider : AIContextProvider
{
    private const string DefaultContextPrompt = "=== TRAVELER PROFILE ===";

    private readonly IChatClient _chatClient;
    private readonly Database _cosmosDatabase;
    private readonly string _containerName;
    private readonly string _contextPrompt;
    private readonly ILogger<UserProfileMemoryProvider>? _logger;
    private readonly UserProfileMemoryProviderScope _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfileMemoryProvider"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client for AI operations.</param>
    /// <param name="cosmosDatabase">The Cosmos DB database.</param>
    /// <param name="containerName">The container name for user profiles.</param>
    /// <param name="scope">Scope values to key the user information storage.</param>
    /// <param name="options">Provider options.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public UserProfileMemoryProvider(
        IChatClient chatClient,
        Database cosmosDatabase,
        string containerName,
        UserProfileMemoryProviderScope scope,
        UserProfileMemoryProviderOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        this._cosmosDatabase = cosmosDatabase ?? throw new ArgumentNullException(nameof(cosmosDatabase));
        this._containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        this._scope = new UserProfileMemoryProviderScope(scope);
        this._logger = loggerFactory?.CreateLogger<UserProfileMemoryProvider>();
        this._contextPrompt = options?.ContextPrompt ?? DefaultContextPrompt;

        if (string.IsNullOrWhiteSpace(this._scope.UserId))
        {
            throw new ArgumentException("UserId must be provided for the scope.", nameof(scope));
        }
    }

    /// <summary>
    /// Gets the user profile from Cosmos DB.
    /// </summary>
    private async Task<UserProfileMemory> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var container = this._cosmosDatabase.GetContainer(this._containerName);
            string documentId = this.GetDocumentId();
            string partitionKey = this._scope.UserId!;

            var response = await container.ReadItemAsync<UserProfileMemory>(
                documentId,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Profile doesn't exist yet, create a new one
            var newProfile = new UserProfileMemory
            {
                Id = this.GetDocumentId(),
                UserId = this._scope.UserId,
                ApplicationId = this._scope.ApplicationId,
                AgentId = this._scope.AgentId
            };
            
            return newProfile;
        }
    }

    /// <summary>
    /// Saves the user profile to Cosmos DB.
    /// </summary>
    private async Task SaveProfileAsync(UserProfileMemory profile, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = this._cosmosDatabase.GetContainer(this._containerName);
            string partitionKey = this._scope.UserId!;

            // Ensure the profile has the correct metadata
            profile.Id = this.GetDocumentId();
            profile.UserId = this._scope.UserId;
            profile.ApplicationId = this._scope.ApplicationId;
            profile.AgentId = this._scope.AgentId;

            await container.UpsertItemAsync(
                profile,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex)
        {
            if (this._logger?.IsEnabled(LogLevel.Error) is true)
            {
                this._logger.LogError(
                    ex,
                    "UserProfileMemoryProvider: Failed to save profile to Cosmos DB. UserId: '{UserId}'.",
                    this._scope.UserId);
            }
            throw;
        }
    }

    /// <summary>
    /// Gets the document ID based on the current scope.
    /// </summary>
    private string GetDocumentId()
    {
        // Use UserId as the primary identifier, with optional ApplicationId and AgentId
        var parts = new List<string> { this._scope.UserId! };
        
        if (!string.IsNullOrWhiteSpace(this._scope.ApplicationId))
        {
            parts.Add(this._scope.ApplicationId);
        }
        
        if (!string.IsNullOrWhiteSpace(this._scope.AgentId))
        {
            parts.Add(this._scope.AgentId);
        }

        return string.Join("_", parts);
    }

    /// <summary>
    /// Checks if the current profile has incomplete information.
    /// </summary>
    private bool HasIncompleteProfile(UserProfileMemory profile)
    {
        return string.IsNullOrEmpty(profile.TravelStyle)
            || string.IsNullOrEmpty(profile.BudgetRange)
            || profile.Interests?.Any() != true
            || !profile.NumberOfTravelers.HasValue
            || string.IsNullOrEmpty(profile.DietaryRequirements);
    }

    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        if (context.InvokeException is not null)
        {
            return; // Do not update profile on failed invocations.
        }

        try
        {
            // Get current profile from Cosmos DB
            UserProfileMemory currentProfile = await this.GetProfileAsync(cancellationToken);

            // Only extract if we have user messages and missing profile data
            if (context.RequestMessages.Any(x => x.Role == ChatRole.User) && this.HasIncompleteProfile(currentProfile))
            {
                if (this._logger?.IsEnabled(LogLevel.Debug) is true)
                {
                    this._logger.LogDebug(
                        "UserProfileMemoryProvider: Extracting profile information. ApplicationId: '{ApplicationId}', AgentId: '{AgentId}', UserId: '{UserId}'.",
                        this._scope.ApplicationId,
                        this._scope.AgentId,
                        this._scope.UserId);
                }

                var result = await this._chatClient.GetResponseAsync<UserProfileMemory>(
                    context.RequestMessages,
                    new ChatOptions
                    {
                        Instructions = """
                    You are extracting traveler profile information from natural conversation to build a comprehensive profile.
                    Extract ONLY information that is explicitly mentioned or strongly implied in the conversation.
                    
                    This is an incremental process - you don't need all fields at once. Extract what's available and return null for missing fields.
                    
                    EXTRACTION RULES:
                    
                    1. **TravelStyle** (string): Identify their travel style
                       - Examples: "budget backpacker", "luxury traveler", "family vacation", "adventure seeker", "cultural explorer", "romantic getaway", "solo traveler"
                       - Extract from phrases like: "I'm a budget backpacker", "we're traveling with kids", "looking for luxury resorts"
                    
                    2. **BudgetRange** (string): Extract budget information
                       - Examples: "$1000-2000 AUD", "3000+ AUD", "budget-friendly", "mid-range", "luxury", "under 50 AUD/day"
                       - Extract from phrases like: "my budget is 2000-3000 AUD", "looking for budget options", "money is no object"
                    
                    3. **Interests** (list): Travel interests and preferred activities - LIMIT TO TOP 3-5
                       - Examples: ["hiking", "beaches"], ["museums", "history"], ["food", "wine"], ["adventure"]
                       - Extract from: "I love hiking", "interested in beaches and coastal areas", "we enjoy museums and history"
                       - Keep only the most important interests; prioritize what they emphasize most
                    
                    4. **PastDestinations** (list of objects with Destination and Rating):
                       - Destination: Name of place visited
                       - Rating: Their sentiment ("loved it", "enjoyed it", "it was okay", "disappointing")
                       - Extract from: "I went to Portugal and loved it", "visited Iceland last year, it was amazing"
                    
                    5. **NumberOfTravelers** (integer): How many people are traveling
                       - Examples: 1, 2, 4, 6
                       - Extract from: "just me", "my partner and I", "family of four", "group of 6 friends"
                    
                    6. **TripDuration** (string): Preferred trip length
                       - Examples: "weekend", "1 week", "2 weeks", "10-14 days", "1 month+", "flexible"
                       - Extract from: "looking for a week-long trip", "2-3 weeks", "long weekend getaway"
                    
                    7. **DietaryRequirements** (string): Dietary requirements or restrictions
                       - Examples: "vegetarian", "vegan", "gluten-free", "halal", "kosher", "dairy-free", "nut allergy", "none"
                       - Extract from: "I'm vegetarian", "I don't eat meat", "I have a gluten allergy", "we follow halal diet", "no dietary restrictions"
                       - Be specific: Distinguish between vegetarian, vegan, pescatarian, etc.
                    
                    EXTRACTION GUIDELINES:
                    - Be conservative: Only extract what is clearly stated or strongly implied
                    - Normalize formats: Convert "three of us" to 3
                    - Return null for any field not mentioned in the conversation
                    - For Interests: Focus on quality over quantity - extract only the most emphasized activities
                    - Don't infer beyond what's said: If they mention "hiking", don't assume "adventure seeker" unless clearly stated

                    """
                    }, cancellationToken: cancellationToken);

                bool profileUpdated = false;

                // Update travel style (only if not set or explicitly changed)
                if (!string.IsNullOrEmpty(result.Result.TravelStyle))
                {
                    currentProfile.TravelStyle = result.Result.TravelStyle;
                    profileUpdated = true;
                }

                // Update budget range (only if not set or explicitly changed)
                if (!string.IsNullOrEmpty(result.Result.BudgetRange))
                {
                    currentProfile.BudgetRange = result.Result.BudgetRange;
                    profileUpdated = true;
                }

                // Merge interests without duplicates (cap at 5 most important)
                if (result.Result.Interests?.Any() == true)
                {
                    currentProfile.Interests ??= new List<string>();
                    foreach (string interest in result.Result.Interests)
                    {
                        if (!currentProfile.Interests.Contains(interest, StringComparer.OrdinalIgnoreCase))
                        {
                            currentProfile.Interests.Add(interest);
                            profileUpdated = true;
                        }
                    }
                    // Cap at 5 interests to avoid list bloat
                    if (currentProfile.Interests.Count > 5)
                    {
                        currentProfile.Interests = currentProfile.Interests.Take(5).ToList();
                    }
                }

                // Update number of travelers (only if not set or explicitly changed)
                if (result.Result.NumberOfTravelers.HasValue && !currentProfile.NumberOfTravelers.HasValue)
                {
                    currentProfile.NumberOfTravelers = result.Result.NumberOfTravelers;
                    profileUpdated = true;
                }

                // Update trip duration (only if not set or explicitly changed)
                if (!string.IsNullOrEmpty(result.Result.TripDuration) && string.IsNullOrEmpty(currentProfile.TripDuration))
                {
                    currentProfile.TripDuration = result.Result.TripDuration;
                    profileUpdated = true;
                }

                // Update dietary restrictions (only if not set or explicitly changed)
                if (!string.IsNullOrEmpty(result.Result.DietaryRequirements) &&
                    string.IsNullOrEmpty(currentProfile.DietaryRequirements))
                {
                    currentProfile.DietaryRequirements = result.Result.DietaryRequirements;
                    profileUpdated = true;
                }

                // Merge past destinations without duplicates
                if (result.Result.PastDestinations?.Any() == true)
                {
                    currentProfile.PastDestinations ??= new List<PastTrip>();
                    foreach (PastTrip trip in result.Result.PastDestinations)
                    {
                        // Check if destination already exists
                        PastTrip? existing = currentProfile.PastDestinations.FirstOrDefault(t =>
                            t.Destination?.Equals(trip.Destination, StringComparison.OrdinalIgnoreCase) == true);

                        if (existing == null)
                        {
                            currentProfile.PastDestinations.Add(trip);
                            profileUpdated = true;
                        }
                        else if (!string.IsNullOrEmpty(trip.Rating) && string.IsNullOrEmpty(existing.Rating))
                        {
                            // Update rating if we didn't have one before
                            existing.Rating = trip.Rating;
                            profileUpdated = true;
                        }
                    }
                }

                // Save updated profile to Cosmos DB
                if (profileUpdated)
                {
                    await this.SaveProfileAsync(currentProfile, cancellationToken);
                    
                    if (this._logger?.IsEnabled(LogLevel.Information) is true)
                    {
                        this._logger.LogInformation(
                            "UserProfileMemoryProvider: Profile updated. ApplicationId: '{ApplicationId}', AgentId: '{AgentId}', UserId: '{UserId}'.",
                            this._scope.ApplicationId,
                            this._scope.AgentId,
                            this._scope.UserId);

                        if (this._logger.IsEnabled(LogLevel.Trace))
                        {
                            this._logger.LogTrace(
                                "UserProfileMemoryProvider: Updated Profile:\n{Profile}\nApplicationId: '{ApplicationId}', AgentId: '{AgentId}', UserId: '{UserId}'.",
                                JsonSerializer.Serialize(currentProfile, new JsonSerializerOptions { WriteIndented = true }),
                                this._scope.ApplicationId,
                                this._scope.AgentId,
                               this._scope.UserId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (this._logger?.IsEnabled(LogLevel.Error) is true)
            {
                this._logger.LogError(
                    ex,
                    "UserProfileMemoryProvider: Failed to extract profile information. ApplicationId: '{ApplicationId}', AgentId: '{AgentId}', UserId: '{UserId}'.",
                    this._scope.ApplicationId,
                    this._scope.AgentId,
                    this._scope.UserId);
            }
        }
    }

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get profile from Cosmos DB
            UserProfileMemory profile = await this.GetProfileAsync(cancellationToken);
            StringBuilder instructions = new();

            // Check if we have meaningful profile data
            bool hasProfileData = !string.IsNullOrEmpty(profile.TravelStyle)
                || !string.IsNullOrEmpty(profile.BudgetRange)
                || profile.Interests?.Any() == true
                || profile.NumberOfTravelers.HasValue
                || !string.IsNullOrEmpty(profile.TripDuration)
                || !string.IsNullOrEmpty(profile.DietaryRequirements)
                || profile.PastDestinations?.Any() == true;

            if (hasProfileData)
            {
                instructions.AppendLine(this._contextPrompt);

                if (!string.IsNullOrEmpty(profile.TravelStyle))
                {
                    instructions.AppendLine($"Travel Style: {profile.TravelStyle}");
                }

                if (!string.IsNullOrEmpty(profile.BudgetRange))
                {
                    instructions.AppendLine($"Budget Range: {profile.BudgetRange}");
                }

                if (profile.Interests?.Any() == true)
                {
                    instructions.AppendLine($"Interests: {string.Join(", ", profile.Interests)}");
                }

                if (profile.NumberOfTravelers.HasValue)
                {
                    instructions.AppendLine($"Number of Travelers: {profile.NumberOfTravelers.Value}");
                }

                if (!string.IsNullOrEmpty(profile.TripDuration))
                {
                    instructions.AppendLine($"Preferred Trip Duration: {profile.TripDuration}");
                }

                if (!string.IsNullOrEmpty(profile.DietaryRequirements))
                {
                    instructions.AppendLine($"Dietary Restrictions: {profile.DietaryRequirements}");
                }

                if (profile.PastDestinations?.Any() == true)
                {
                    instructions.AppendLine("Past Trips:");
                    foreach (PastTrip trip in profile.PastDestinations)
                    {
                        string tripInfo = $"  • {trip.Destination}";
                        if (!string.IsNullOrEmpty(trip.Rating))
                        {
                            tripInfo += $" ({trip.Rating})";
                        }
                        instructions.AppendLine(tripInfo);
                    }
                }


                string injectedInstructions = instructions.ToString();

                if (this._logger?.IsEnabled(LogLevel.Information) is true)
                {
                    this._logger.LogInformation(
                        "UserProfileMemoryProvider: Injecting profile context. ApplicationId: '{ApplicationId}', AgentId: '{AgentId}', UserId: '{UserId}'.",
                        this._scope.ApplicationId,
                        this._scope.AgentId,
                        this._scope.UserId);

                    if (this._logger.IsEnabled(LogLevel.Trace))
                    {
                        this._logger.LogTrace(
                            "UserProfileMemoryProvider: Injected Instructions:\n{Instructions}\nApplicationId: '{ApplicationId}', AgentId: '{AgentId}', UserId: '{UserId}'.",
                            injectedInstructions,
                            this._scope.ApplicationId,
                            this._scope.AgentId,
                            this._scope.UserId);
                    }
                }

                return new AIContext
                {
                    Instructions = injectedInstructions
                };
            }

            return new AIContext();
        }
        catch (Exception ex)
        {
            if (this._logger?.IsEnabled(LogLevel.Error) is true)
            {
                this._logger.LogError(
                    ex,
                    "UserProfileMemoryProvider: Failed to inject profile context. ApplicationId: '{ApplicationId}', AgentId: '{AgentId}', UserId: '{UserId}'.",
                    this._scope.ApplicationId,
                    this._scope.AgentId,
                    this._scope.UserId);
            }
            return new AIContext();
        }
    }
}

internal sealed class UserProfileMemoryProviderScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfileMemoryProviderScope"/> class.
    /// </summary>
    public UserProfileMemoryProviderScope() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfileMemoryProviderScope"/> class by cloning an existing scope.
    /// </summary>
    /// <param name="sourceScope">The scope to clone.</param>
    public UserProfileMemoryProviderScope(UserProfileMemoryProviderScope sourceScope)
    {
        //Throw.IfNull(sourceScope);

        this.ApplicationId = sourceScope.ApplicationId;
        this.AgentId = sourceScope.AgentId;
        this.UserId = sourceScope.UserId;
    }

    /// <summary>
    /// Gets or sets an optional ID for the application to scope user information to.
    /// </summary>
    /// <remarks>If not set, the scope of the user information will span all applications.</remarks>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets an optional ID for the agent to scope user information to.
    /// </summary>
    /// <remarks>If not set, the scope of the user information will span all agents.</remarks>
    public string? AgentId { get; set; }

    /// <summary>
    /// Gets or sets an optional ID for the user to scope user information to.
    /// </summary>
    /// <remarks>If not set, the scope of the user information will span all users.</remarks>
    public string? UserId { get; set; }
}

internal sealed class UserProfileMemoryProviderOptions
{
    /// <summary>
    /// When providing user profile information to the model, this string is prefixed to the profile data to supply context.
    /// </summary>
    /// <value>Defaults to "=== TRAVELER PROFILE (Remember and use this information) ===".</value>
    public string? ContextPrompt { get; set; }
}