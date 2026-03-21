# Lab 2: Remember Everything - Long-Term Memory with Vector Search

In this lab, you will level up your travel assistant to recall details from hundreds of past conversations, overcoming the limitations of context windows in Large Language Models (LLMs).

By the end of this lab, you will:

- ✅ Implement vector storage for conversation history persistence
- ✅ Enable semantic search across all past conversations
- ✅ Overcome context window limitations

## The Problem: Limited Context Windows & Lost Conversations

After Lab 1, agents can remember structured profiles, but lose conversational details:

```text

👤 User: I am planning a trip to New Zealand with a budget of around $3,000. 
          I am keen to do some hiking this time.
          Any destination recommendations?

```

```text

🤖 Agent: Absolutely! Based on your interests, I recommend:

        Fiordland National Park (Milford Track)
        One of the world’s best hiking trails with dramatic fjords, waterfalls, and pristine forests.
        Perfect for multi-day hikes.
        Best time: November to March (pleasant weather for hiking).

        Aoraki/Mount Cook National Park
        Home to hiking tracks like the Hooker Valley Track with stunning views of glaciers and New Zealand’s tallest mountain, Mount Cook.
        Perfect for breathtaking day hikes with alpine landscapes.

    Let me know which of these destinations appeals to you, or if you'd like more details!

```

```text

👤 User: Thanks! I'll check out.

```

```text

[Several conversations later]

```

```text

👤 User: What places did you recommend to me last time in New Zealand?

```

```text

🤖 Agent: Sorry, I don't have that information. 

          ❌ Beyond context window!

```

---

## Sample Interaction: What You'll Build

```text

👤 User: I am planning a trip to New Zealand with a budget of around $3,000. 
          I am keen to do some hiking this time.
          Any destination recommendations?

```

```text

🤖 Agent: Absolutely! Based on your interests, I recommend:

        Fiordland National Park (Milford Track)
        One of the world’s best hiking trails with dramatic fjords, waterfalls, and pristine forests.
        Perfect for multi-day hikes.
        Best time: November to March (pleasant weather for hiking).

        Aoraki/Mount Cook National Park
        Home to hiking tracks like the Hooker Valley Track with stunning views of glaciers and New Zealand’s tallest mountain, Mount Cook.
        Perfect for breathtaking day hikes with alpine landscapes.

    Let me know which of these destinations appeals to you, or if you'd like more details!

```

```text

[Save conversation to vector memory]

```

```text

[Several conversations and days later]

```

```text

👤 User: What places did you recommend to me last time in New Zealand?

```

```text

🤖 Agent: Hi John! Last time, based on your adventurous spirit and love for hiking, 
          I suggested Fiordland National Park in New Zealand. 

```

---

## Instructions

### Step 1: Locate the Source Code

Navigate to the backend project:

``` text

📁 src/backend/

```

You will be modifying the code in this directory to implement vector-based memory.

### Step 2: Update Agent Instructions

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

1. Cosmos DB is used as the vector store for persisting chat history.
2. The `AIContextProviderFactory` is configured to use `CosmosDbChatHistoryProvider` for vector-based memory. The provider is set up to store and retrieve chat messages based on `ApplicationId` and `UserId`.

## Test Your Implementation

1. Refer to the **Running the Application Locally** section in the [Setup Instructions](00-setup_instructions.md#running-the-application-locally) and start running both the backend and frontend.

    If the application is already running, restart the backend server by stopping it (Ctrl + C) and running `dotnet run` again in the `src/backend` directory.

2. Test the agent's memory capabilities with the following prompts:

    Start the conversation with:

    ```text
    I’m planning a trip to New Zealand with a budget of around $3,000. 
    I’m keen to do some hiking this time. 
    Any destination recommendations?
    ```

    *Expected Response:* Agent recommends destinations in New Zealand.

3. Start a new conversation by clicking on **New Chat** in the frontend UI, then ask:

    ```text
    What places did you recommend to me last time in New Zealand?
    ```

    *Expected Response:* Agent recalls the recommended destinations and provides details.

---
