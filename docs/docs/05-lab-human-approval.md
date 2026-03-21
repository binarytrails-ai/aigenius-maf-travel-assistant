# Lab 5: Human-in-the-Loop - Approval Workflows

In this lab, you'll implement human approval workflows for your travel assistant to ensure critical decisions require explicit user consent from the user before execution.

This is important for actions that share sensitive information or perform an action on behalf of the user, such as searching for flights or booking reservations.

By the end of this lab, you will:

- ✅ Enable users to review and approve tool calls before execution
- ✅ Provide clear context for approval decisions

---

## The Problem: Autonomous Actions Without Consent

When searching for flights, the agent sends user details (travel dates, destinations, preferences) to third-party APIs. Without a formal approval workflow, the agent can share this information immediately based solely on the user's request.

This raises privacy and trust concerns, as users may not expect their personal information to be shared without explicit consent.

```text
👤 User: Find flights from Melbourne to Wellington leaving next Friday
```

❌ **Without an approval workflow, the agent proceeds to search for flights immediately:**

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

## Sample Interaction: What You'll Build

The UI will prompt the user to approve or reject critical actions before they are executed.
The user can then approve or reject the action. The response is then added back into the message history for the agent to continue processing.

This approach provides a reliable and secure mechanism to ensure that sensitive actions only execute with explicit user consent.

```text
👤 User: Find flights from Melbourne to Wellington leaving next Friday
```

✅ **With approval workflow, the agent pauses and requests approval:**

```text

🔧 Agent requests approval to call the tool

   Tool: SearchFlights
   Parameters: { 
     origin: "Melbourne",
     destination: "Auckland",
     departureDate: "2026-02-05",
     returnDate: "2026-02-26"
   }

```

```text
   
   [UI shows approval dialog]
   
   ✅ Approve  ❌ Reject

```

```text

👤 User: [Clicks Approve]

```

```text
🔧 Agent calls: SearchFlights(
     origin: "Melbourne",
     destination: "Auckland",
     departureDate: "2026-02-05",
     returnDate: "2026-02-26"
   ) ✅
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

You will be modifying the code in this directory to implement human approval workflows.

### Step 2: Update Agent Definition

Update the `CreateAsync` method in `Agents/ContosoTravelAgentFactory.cs` to wrap SearchFlights with approval requirement.

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

                        #pragma warning disable MEAI001
                        new ApprovalRequiredAIFunction(
                            AIFunctionFactory.Create(_flightFinderTools.SearchFlights)
                        )
                        #pragma warning restore MEAI001
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
        return new ServerFunctionApprovalAgent(agent, _jsonSerializerOptions);
    }
    
```

## Key Implementation Details

The approval workflow consists of following components that work together:

1. **ApprovalRequiredAIFunction** - `ApprovalRequiredAIFunction` is a wrapper around tools that must not be executed automatically. Instead of running the tool immediately, it intercepts the invocation and blocks execution until an explicit approval decision is received.

    ```csharp
    // Wrap the flight search tool with approval requirement
    new ApprovalRequiredAIFunction(
        AIFunctionFactory.Create(TravelSearchTools.SearchFlights))
    ```

2. **ServerFunctionApprovalAgent** - `ServerFunctionApprovalAgent` is the base class for all agents. It acts as a bridge between tool calls and the frontend approval experience. 

    It reads the response from the LLM, to detect when an approval is required. It then sends an event to the frontend so a user can make a decision.

    ```csharp
    // Wrap the agent to enable approval handling
    return new ServerFunctionApprovalAgent(agent, jsonSerializerOptions);
    ```

3. **AG-UI Frontend Integration** - The frontend implements the human approval experience using CopilotKit’s `useHumanInTheLoop` hook. It listens for events emitted by the server and renders a custom approval dialog for the user for the event `request_approval`.

    It captures the user’s decision and sends it back to the server referencing the original approval request identifier. This allows the agent to resume execution based on the user’s choice.

    ```typescript
    // In your React component (src/frontend/app/page.tsx)
    useHumanInTheLoop({
        name: "request_approval", // This should match the tool name in the backend that requires approval
        parameters: [
        {
            name: "request",
            type: "string",
            description: "The approval request containing function details",
            required: true,
        },
        ],
        render: ({ args, respond, status }) => {
        // Parse the approval request from the wrapper
        let approvalData: {
            approvalId?: string;
            toolName?: string;
        } = {};

        if (args.request) {
            try {
            const parsed =
                typeof args.request === "string"
                ? JSON.parse(args.request)
                : args.request;
            console.log("Parsed approval request:", parsed);

            // Handle both snake_case (from backend) and PascalCase (legacy)
            const functionName = parsed.function_name || parsed.FunctionName;
            const approvalId = parsed.approval_id || parsed.ApprovalId;

            console.log("Function name:", functionName);
            console.log("Approval ID:", approvalId);

            approvalData = {
                toolName: functionName,
                approvalId: approvalId,
            };
            console.log("Extracted approval data:", approvalData);
            } catch (e) {
            console.error(
                "Failed to parse approval request:",
                e,
                "Raw args:",
                args,
            );
            }
        } else {
            console.warn("No request property in args:", args);
        }

        return (
            <ApprovalUI args={approvalData} respond={respond} status={status} />
        );
        },
    });
    ```

### How It Works

1. User requests an action that requires approval (e.g., searching for flights).
2. The agent decides which tool to use (e.g., SearchFlights).
3. The tool is wrapped with `ApprovalRequiredAIFunction`, so it does not execute immediately.
4. An approval request is sent to the frontend with approval ID, function name, and parameters.
5. The frontend notices the approval request and displays a custom approval dialog with search parameters.
6. The user approves or rejects the search.
7. The decision is sent back to the agent with the approval request ID.
8. The agent continues the flight search or cancels based on the user's decision.

---

## Test Your Implementation

1. Refer to the **Running the Application Locally** section in the [Setup Instructions](00-setup_instructions.md#running-the-application-locally) and start running both the backend and frontend.

    If the application is already running, restart the backend server by stopping it (Ctrl + C) and running `dotnet run` again in the `src/backend` directory.

2. Test the complete approval workflow with the following prompts:

    **Initial Request**

    Start the conversation with:

    ```
    Find flights from Melbourne to Wellington leaving next Friday
    ```
    
    *Expected Response:* Agent requests approval to call `SearchFlights`.

3. Click the "Approve" button in the UI when the approval dialog appears
    
    *Expected: Agent calls `SearchFlights()` successfully, presents flight options*

---
