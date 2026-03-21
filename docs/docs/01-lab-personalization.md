# Lab 1: Remember Me - Personalization & Memory

In this lab, you will level up your travel assistant by adding personalization capabilities.

You will implement a custom memory provider that allows the agent to remember user preferences, and interests across multiple sessions.

By the end of this lab, you will:

- ✅ Enable your agent to extract and store user preferences from natural conversation
- ✅ Create personalized travel recommendations based on stored preferences
- ✅ Understand cross-session memory persistence

## The Problem: Generic, Forgetful Agents

```text
👤 User: I want to plan a trip
```

```text
🤖 Agent: I'd love to help! What's your budget?
```

```text
👤 User: Usually around $2,000
```

```text
🤖 Agent: Great! What kind of experiences do you enjoy?
```

```text
👤 User: I love hiking and coastal walks
```

```text
🤖 Agent: Sounds wonderful! Based on your interests, here are some destinations that combine hiking with coastal beauty:

• Great Ocean Road, Victoria
  - Dramatic coastal cliffs and rainforest trails
  - Perfect for: Scenic coastal walks, wildlife spotting
  - Best time: October-April
  - Budget: Well within $2,000

• Sunshine Coast, Queensland
  - Beach trails and hinterland rainforest hikes
  - Perfect for: Relaxed coastal atmosphere, great food scene
  - Best time: Year-round (mild winters)
  - Budget: Mid-range, fits your budget

Which of these appeals to you?
```

```text
[Next day, new conversation]
```

```text
👤 User: Suggest a vacation for me
```

```text
🤖 Agent: Where would you like to go? ❌ Forgot everything!
```

---

## Sample Interaction: What You'll Build

```text
👤 User: I want to plan a trip
```

```text
🤖 Agent: I'd love to help! What's your budget for this trip?
```

```text
👤 User: Usually around $2,000
```

```text
Stored to memory:
• Budget: ~$2,000
```

```text
🤖 Agent: Great! What kind of experiences do you enjoy?
```

```text
👤 User: I love hiking and coastal walks
```

```text
Stored to memory:
• Interests: Hiking, coastal walks
```

```text
🤖 Agent: Sounds wonderful! Based on your interests, here are some destinations that combine hiking with coastal beauty:

• Great Ocean Road, Victoria
  - Dramatic coastal cliffs and rainforest trails
  - Perfect for: Scenic coastal walks, wildlife spotting
  - Best time: October-April
  - Budget: Well within $2,000

• Sunshine Coast, Queensland
  - Beach trails and hinterland rainforest hikes
  - Perfect for: Relaxed coastal atmosphere, great food scene
  - Best time: Year-round (mild winters)
  - Budget: Mid-range, fits your budget

Which of these appeals to you?
```

```text
Stored to memory:
• Budget: ~$2,000
• Interests: Hiking, coastal walks
```

```text
[Next day, new conversation]
```

```text
👤 User: Suggest a vacation for me
```

```text

🤖 Agent: Hi again, John! Based on your love for hiking and coastal walks, 
          and your $2,000 budget, 
          I'd love to help plan another wonderful adventure for you! 

          Would you like me to use these same preferences, 
          or are you looking for something different this time?

```

```text
👤 User: Yes, use the same preferences
```

```text
🤖 Agent: Perfect! Here are some destinations that match your hiking and coastal interests:

• Great Ocean Road, Victoria
  - Dramatic coastal cliffs and rainforest trails
  - Best time: October-April
  - Budget: Fits your $2,000 budget

• Sunshine Coast, Queensland
  - Beach trails and hinterland rainforest hikes
  - Perfect for: Relaxed coastal atmosphere
  - Best time: Year-round
  - Budget: Mid-range, within budget

Which of these appeals to you?
```

---

## Instructions

### Step 1: Locate the Source Code

Navigate to the backend project:

``` text

📁 src/backend/

```

You will be modifying the code in this directory to implement personalization features.

### Step 2: Update Agent Definition

Open `Agents/ContosoTravelAgentFactory.cs` and update the agent instructions to enable memory awareness:

```csharp
    private const string AgentInstructions = """
    You are Contoso Travel Assistant, an intelligent assistant for Contoso Travel Agency.
    Introduce yourself as "Contoso Travel Assistant" and help travelers with all their travel needs!

    # ROLE
    - Help travelers discover destinations
    - Provide personalized destination recommendations based on their preferences
    - Offer travel advice on destinations, timing, activities, and costs
    - Be friendly, enthusiastic, conversational, and knowledgeable about travel

    ## TOOLS

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

    ## PROFILE USAGE (All Interactions)
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
    """;
```

### Step 3: Update Agent Definition

Now update the `CreateAsync` method to integrate the conversation history memory provider:

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
              Tools = []
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


              return userProfileMemoryProvider;
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

The `UserProfileMemoryProvider` class manages personalization by intercepting messages before and after they are sent to the Language Model. It is registered in the agent via the `AIContextProviderFactory`.

- **After Receiving from LLM**: Analyzes the user's message to extract new profile information and updates storage automatically
- **Before Sending to LLM**: Loads the user's profile from storage and injects it into the conversation context
   This is a sample message prepended to the agent's prompt:

    ``` text

    {System prompt...}

    === TRAVELER PROFILE ===
    Travel Style: Budget backpacker
    Budget Range: $2,000–$2,000
    Dietary Restrictions: Vegetarian
    Interests: Hiking, coastal trails, wildlife watching
    Places They Want to Visit: Blue Mountains (NSW), Grampians National Park (VIC)
    Number of Travelers: 2
    Preferred Trip Duration: 1-2 weeks
    Past Trips:
      • Tasmania (March 2024) - Loved it
        Highlights: Three Capes Track, Wineglass Bay, Bay of Fires, wildlife encounters

    ```

### How It Works

1. **Profile Extraction**: The provider passes conversation history to the LLM with specific extraction instructions. The LLM identifies profile information (travel style, interests, dietary restrictions, etc.) from natural conversation. 

    The following data structure is used for the user profile:

    ```csharp
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

    ```

2. **Structured Storage**: The Profile data is returned in a structured JSON format for easy parsing and storage. This ensures consistency and reliability.

3. **Contextual Guidance**: The provider appends additional instructions to the agent's prompt to ensure it uses the stored profile effectively when making recommendations.

4. **Data Isolation**: The memory provider is scoped by user ID, agent ID, and application ID to ensure that each user's data is kept separate and secure.

## Test Your Implementation

1. Refer to the **Running the Application Locally** section in the [Setup Instructions](00-setup_instructions.md#running-the-application-locally) and start running both the backend and frontend.

    If the application is already running, restart the backend server by stopping it (Ctrl + C) and running `dotnet run` again in the `src/backend` directory.

2. Test the personalization workflow with the following prompts:

    **Initial Conversation - Building Profile**

    Start the conversation with:

    ```text
    Hi! Can you help me plan a trip ?
    ```

    *Expected Response:* Agent greets you and asks about your travel preferences (e.g., budget, travel style, interests).

3. Answer the agent's questions to build your profile.

    *Expected Response:* Agent provides personalized destination recommendations and stores your profile information (travel style, budget, interests, past trips, places to visit).

4. Test that the agent remembers your preferences in a new session.

    Start a new conversation by clicking on **New Chat** in the frontend UI, then ask:

    ```text
    I want to plan my next vacation
    ```

    *Expected Response:* Agent references your stored profile and provides personalized recommendations based on your preferences.*
    
---
