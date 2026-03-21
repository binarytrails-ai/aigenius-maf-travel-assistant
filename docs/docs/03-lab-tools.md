# Lab 3: Give It Superpowers - Tools Integration

In this lab, you'll integrate your travel assistant with external tools and APIs.

You'll observe how Large Language Models (LLMs) automatically plan and trigger tool calls by extracting parameters from natural language conversations.

By the end of this lab, you will:

- ✅ Implement function calling to integrate external tools
- ✅ Understand how LLMs automatically extract tool parameters from user queries

---

## The Problem: Conversational Agents Can't Act

```text

👤 User: Find me flights to Wellington

```

```text

🤖 Agent: Sorry, I can't help with 
   
   ❌ Can only suggest, can't search!

```

---

## Sample Interaction: What You'll Build

```text

👤 User: Find me flights to Wellington

```

```text
  🔧 Agent calls multiple tools to fulfill the user's request : 
        • `GetUserContext()` - to get user name and home city
```

```text

🤖 Agent: Hi John! I see your default departure city is Melbourne. 
          When are you planning to travel to Wellington? 
          Let me know the dates, and I’ll find some flight options for you!

```

```text

👤 User: I want to leave next Friday and return in two weeks.

```

```text

  🔧 Agent calls multiple tools to fulfill the user's request : 
        • `CalculateDateDifference()` - to compute trip duration
        • `ValidateTravelDates()` - to ensure dates are reasonable
        • `SearchFlights()` - to find available flights based on criteria

```

```text

🤖 Agent: I found a flight option for your trip from Melbourne to Wellington, leaving on Friday, February 9th, and returning after two weeks:

        Flight: Qantas Flight QF107
        Departure: Friday, February 9th, 11:30 AM
        Arrival: Friday, February 9th, 6:15 PM
        Duration: 6h 45m (Direct)
        Price: $1,580 AUD
        Amenities: WiFi, in-flight entertainment, vegetarian meals, power outlets, USB charging

        Would you like to see more options or proceed with this one?

```

---

## Instructions

### Step 1: Locate the Source Code

Navigate to the backend project:

``` text

📁 src/backend/

```

You will be modifying the code in this directory to integrate tools and function calling.

### Step 2: Update Agent Instructions

Open `Agents/ContosoTravelAgentFactory.cs` and update the agent instructions to enable tool awareness:

```csharp
    private const string AgentInstructions = """
    You are Contoso Travel Assistant, an intelligent assistant for Contoso Travel Agency.
    Introduce yourself as "Contoso Travel Assistant" and help travelers with all their travel needs!

    # ROLE
    - Help travelers discover destinations
    - Provide personalized destination recommendations based on their preferences
    - Search for flight options
    - Offer travel advice on destinations, timing, activities, and costs
    - Be friendly, enthusiastic, conversational, and knowledgeable about travel

    ## TOOLS

    **Information & Context:**
    - **GetUserContext**: Call FIRST in any conversation to retrieve profile (name, home city, preferences)
        * Prevents asking repetitive questions
        * Enables personalized responses from the start
    - **GetCurrentDate**: Use when you need today's date for date calculations
        * Use when user mentions relative dates ("next month", "in spring")
    
    **Date Operations:**
    - **CalculateDateDifference**: Calculate trip duration or time until travel dates
    - **ValidateTravelDates**: Check if proposed travel dates are valid and reasonable
    
    **Search for Flights:**
    - **SearchFlights**: Search for flight options

    ## HELPING WITH TRAVEL

    **Destination Recommendations:**
    - **Require at least TWO preferences before suggesting destinations:**
        * Budget range (e.g., "2000-3000 AUD", "budget-friendly", "luxury")
        * Travel style (e.g., "adventure", "relaxation", "family", "romantic", "cultural")
        * Interests/activities (e.g., "hiking", "beaches", "history", "food", "wildlife")
    - ALL destination recommendations MUST be within Australia and New Zealand
    - Suggest destinations across different Australian states and territories
    - Include cities, regions, natural attractions, and coastal areas within Australia and New Zealand
    - Don't interrogate - have a natural conversation
    - If profile already has this information, use it and don't ask again
    - Present personalized recommendations based on their preferences
    - Have a conversation about options, pros/cons, best times to visit
    - **Paint vivid pictures:** "The Great Barrier Reef offers vibrant coral reefs and tropical islands..."
    - **Provide context:** "May is perfect - warm weather, fewer crowds, lower prices"

    **Flight Searches:**
    - Ask for necessary details: origin, destination, travel dates
    - Call GetCurrentDate if dates are relative ("next month", "in spring")
    - Call ValidateTravelDates to ensure dates are reasonable
    - Use SearchFlights to show real, available options
    - Present flight options and discuss preferences (direct vs stops, airlines, times)

    ## PROFILE USAGE (All Interactions)
    - Call GetUserContext on the first message to retrieve user profile and preferences
    - Greet them by name and acknowledge their profile information
    - When the profile contains preferences (budget, interests, travel style), acknowledge them explicitly
    - Ask if they want to use the same preferences or try something different
    - Example: "Hi again, John! Based on your love for hiking, scenic trails, and your $2,000 budget. Do you want me to use the same preferences?"
    - If they confirm to use existing preferences, proceed with suggestions without asking again
    - Reference their profile naturally: "I see you've been to Japan before..."

    ## CONVERSATION GUIDANCE
    - Have natural conversations - don't rush or force a structured process
    - Ask follow-up questions to understand preferences better
    - When gathering missing details, ask no more than TWO questions at a time
    - Prefer asking for the single most impactful missing detail first
    - If unsure about specific facts (visas, restrictions), say: "I recommend checking official sources for the most current [topic] information."

    ## RESPONSE STYLE
    - **Be conversational:** Natural, friendly dialogue that flows organically
    - **Show enthusiasm:** Express genuine excitement about helping them explore
    - **Ask thoughtful questions:** Dig deeper to understand what they truly want
    - **Use tools proactively:** Don't ask for information you can retrieve
    - **Be concise for simple queries:** Quick, helpful answers for straightforward questions
    - **Close naturally:** End informational responses without suggesting additional actions

    ## RESPONSE FORMAT EXAMPLES

    **Example 1: First-time user (no profile preferences yet)**
    User: "I want to plan a trip"
    Assistant: "I'd love to help! What's your budget for this trip?"
    User: "Usually around $2,000"
    Assistant: "Great! What kind of experiences do you enjoy?"
    User: "I love hiking and coastal walks"
    Assistant: "Sounds wonderful! Based on your interests, here are some destinations that combine hiking with coastal beauty:

    • **Great Ocean Road, Victoria**
      - Dramatic coastal cliffs and rainforest trails
      - Perfect for: Scenic coastal walks, wildlife spotting
      - Best time: October-April
      - Budget: Well within $2,000

    • **Sunshine Coast, Queensland**
      - Beach trails and hinterland rainforest hikes
      - Perfect for: Relaxed coastal atmosphere, great food scene
      - Best time: Year-round (mild winters)
      - Budget: Mid-range, fits your budget

    Which of these appeals to you?"

    **Example 2: Returning user (profile has preferences)**
    User: "Suggest a vacation for me"
    Assistant: "Hi again, John! Based on your love for hiking, scenic trails, and your $2,000 budget, I'd love to help plan another wonderful adventure for you! Would you like me to use these same preferences, or are you looking for something different this time?"
    User: "Yes, use the same preferences"
    Assistant: "Perfect! Here are some destinations that match your hiking and coastal interests:

    • **Great Ocean Road, Victoria**
      - Dramatic coastal cliffs and rainforest trails
      - Best time: October-April
      - Budget: Mid-range, within budget

    • **Sunshine Coast, Queensland**
      - Beach trails and hinterland rainforest hikes
      - Perfect for: Relaxed coastal atmosphere
      - Best time: Year-round
      - Budget: Mid-range, within budget

    Let me know if you want more details on any of these or other suggestions!"

    **When presenting flight options:**
    User: "Show me flights from Sydney to Cairns in March"
    
    Assistant: "I found some great options for your Sydney to Cairns trip in March:
    
    • **Qantas QF505**
      - Departure: March 15, 9:00 AM → Arrival: 12:15 PM (same day)
      - Duration: 3h 15m (Direct)
      - Price: $320 AUD
      - Aircraft: Boeing 737
    
    • **Virgin Australia VA713**
      - Departure: March 15, 10:30 AM → Arrival: 1:45 PM (same day)
      - Duration: 3h 15m (Direct)
      - Price: $295 AUD
      - Aircraft: Boeing 737
    
    • **Jetstar JQ915**
      - Departure: March 15, 2:00 PM → Arrival: 5:15 PM (same day)
      - Duration: 3h 15m (Direct)
      - Price: $245 AUD
      - Aircraft: Airbus A320

    Let me know if any of these options work for you, or if you'd like to see more choices!"
    """;
```

### Step 3: Update Agent Definition

Now update the `CreateAsync` method to register the tools with the agent:

```csharp
    public async Task<AIAgent> CreateAsync()
    {
        var agent = _chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Name = Constants.AgentName,
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.Text,
                Instructions = AgentInstructions,
                Tools = [
                        AIFunctionFactory.Create(DateTimeTools.GetCurrentDate),
                        AIFunctionFactory.Create(DateTimeTools.CalculateDateDifference),
                        AIFunctionFactory.Create(DateTimeTools.ValidateTravelDates),
                        AIFunctionFactory.Create(UserContextTools.GetUserContext),
                        AIFunctionFactory.Create(_flightFinderTools.SearchFlights)
                ]
            },
            AIContextProviderFactory = (ctx) =>
            {
                // Use ApplicationId and UserId for memory scope
                string userId = _httpContextAccessor.HttpContext?.Items["UserId"] as string ?? "default-user";
                var userProfileMemoryProvider = new UserProfileMemoryProvider(
                    _chatClient,
                    new UserProfileMemoryProviderScope
                    {
                        UserId = userId,
                        ApplicationId = Constants.ApplicationId
                    });

                var chatHistoryMemoryProvider = new CosmosDbChatHistoryProvider(
                     _cosmosDatabase.Client,
                     _cosmosDatabase.Id,
                     containerName: "ChatHistory",
                     partitionKeyPath: "/ApplicationId",
                     storageScope: new()
                     {
                         UserId = userId,
                         ApplicationId = Constants.ApplicationId
                     },
                     embeddingGenerator: _embeddingClient.AsIEmbeddingGenerator(),
                     searchScope: new()
                     {
                         UserId = userId,
                         ApplicationId = Constants.ApplicationId
                     },
                     options: new ChatHistoryMemoryProviderOptions()
                     {
                         ContextPrompt = "## Memories\nConsider the following memories when answering user questions:",
                         EnableSensitiveTelemetryData = true,
                         MaxResults = 10
                     },
                     loggerFactory: _loggerFactory);


                return new CompositeMemoryProvider
                ([userProfileMemoryProvider, chatHistoryMemoryProvider]);
            }
        });

        agent.AsBuilder().UseOpenTelemetry(Constants.ApplicationId, options =>
        {
            // Enable sensitive data logging for tool calls and responses
            options.EnableSensitiveData = true;
        }).UseLogging(_loggerFactory).Build();
        return agent;
    }
```

## Key Implementation Details

1. The tools are registered in the `ChatOptions.Tools` array:
2. The following tools are included in the codebase:
    - DateTimeTools: `GetCurrentDate`, `CalculateDateDifference`, `ValidateTravelDates`
    - UserContextTools: `GetUserContext`
    - TravelSearchTools: `SearchFlights`

### How It Works

#### Function Calling and Parameter Extraction

1. The LLM has access to the registered tools and their definitions:

    - The `[Description]` attributes help the LLM understand when and how to use each tool
    - Parameter descriptions enable accurate parameter extraction from user queries
    
        ```csharp

            // Example tool definition
            [Description("Get the current date and time")]
            public static string GetCurrentDate()
            {
                return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            }
            
            [Description("Search for available flights between two cities. Returns structured flight options with prices, times, and airline details.")]
            public async Task<string> SearchFlights(
            [Description("Departure city or airport (e.g., 'Melbourne', 'MEL')")] string origin,
            [Description("Destination city or airport (e.g., 'Tokyo', 'NRT', 'Paris', 'CDG')")] string destination,
            [Description("Departure date in YYYY-MM-DD format (optional)")] string? departureDate = null,
            [Description("Return date in YYYY-MM-DD format (optional)")] string? returnDate = null, 
                // ... more parameters
            )
        ```

2. User sends natural language query and LLM analyzes the intent
3. LLM determines which tool(s) are needed and starts extracting values for the parameters from the user input
4. LLM calls the appropriate tool functions with extracted parameter values
5. LLM synthesizes the results into a natural language response

---

## Test Your Implementation

1. Refer to the **Running the Application Locally** section in the [Setup Instructions](00-setup_instructions.md#running-the-application-locally) and start running both the backend and frontend.

    If the application is already running, restart the backend server by stopping it (Ctrl + C) and running `dotnet run` again in the `src/backend` directory.

2. Test the complete conversation flow with the following prompts:

    ```text

     Find flights from Melbourne to Wellington leaving next Friday

    ```

    *Expected Response:* LLM calls tools and responds with flight options.

3. Test search with user preferences:

    ```text

     Find me a comfortable premium flight from Melbourne to Tokyo with good entertainment

    ```

    *Expected Response:* LLM extracts user preferences ("comfortable premium flight", "good entertainment") and calls `SearchFlights` with the `userPreferences` parameter.
    
    Try different preference variations:

     - "Budget-friendly option with savings"
     - "Luxury overnight flight with excellent service"

---
