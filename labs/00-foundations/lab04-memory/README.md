# Lab 04: Travel Assistant with Long-Term Memory

In this lab, you will extend your travel assistant with **long-term memory** that persists across sessions.

You will implement in-memory storage for user preferences like seat and meal choices, allowing the agent to remember and personalize experiences.  In production, you would use a database or external storage, but for simplicity, this lab uses in-memory storage. 

By the end of this lab, you will:

- Understand the use of long-term memory in agents
- Use the LLM to extract structured preference data from conversation
- Build an agent that provides personalized experiences across sessions

## Key Implementation Details

### Session Memory vs. Long-Term Memory

In the previous labs, a session (`AgentSession`) kept the conversation history alive for a single run. But once the program restarts, that history is gone.

**Long-term memory** is different — it stores information about the user that *persists across sessions*. Every new conversation can start with the agent already knowing the user's preferences, as if it remembered them from last time.

This lab uses an in-memory store for simplicity (the preferences are held in a `UserTravelPreferences` object), but the pattern is identical to one backed by a database. Only the storage layer changes.

### The Two-Phase Memory Provider

`TravelPreferencesMemory` extends `AIContextProvider` and implements **both phases** of the lifecycle:

- **Before the call (`ProvideAIContextAsync`)** — serialises the stored preferences into instructions and injects them so the agent personalises its response
- **After the call (`StoreAIContextAsync`)** — reads the conversation and uses the LLM to extract any newly expressed preferences, then updates the in-memory store

This is the key insight: the **model itself does the extraction work**. Rather than parsing the user's message with custom rules, the code sends the conversation back to the LLM with a structured extraction prompt and asks it to return a `UserTravelPreferences` JSON object.

### The `UserTravelPreferences` Model

Preferences are stored as a typed C# record:

```csharp
internal sealed class UserTravelPreferences
{
    public string? DestinationTypes { get; set; }
    public string? TravelStyle { get; set; }
    public string? SeatPreference { get; set; }
    public string? MealPreference { get; set; }
    public string? OtherPreferences { get; set; }
}
```

Having a strongly-typed model makes the extraction prompt precise. The LLM is asked to fill in only these specific fields from the conversation. Any field left empty means no preference was expressed for it.

---

## Instructions

### Step 1: Navigate to the Lab Folder

```bash
cd labs/00-foundations/lab04-longterm-memory
```

### Step 2: Run the Program

With .NET 10's file-based apps, you can run the single .cs file directly:

```bash
dotnet run Program.cs
```

Or in Visual Studio Code, open Program.cs and click the **"Run"** button that appears above the code.

### Step 3: Observe the Output

You should see the agent remember your preferences across multiple sessions. Notice how:

- The agent extracts preferences from natural conversation (e.g., "I prefer window seats and vegetarian meals")
- The agent personalizes responses based on stored preferences

---

## Challenges and Next Steps

**Add a new preference field**
- Add a `BudgetLevel` property to `UserTravelPreferences` (e.g. `"budget"`, `"mid-range"`, `"luxury"`).
- Update the extraction prompt inside `StoreAIContextAsync` to look for budget signals, then update the context builder to include it.
- Test with: `"I usually travel on a tight budget, under $1000 per trip"`.

**Test preference overwriting**
- Start with the hardcoded vegetarian meal preference.
- In a follow-up message, say: `"Actually I've switched to eating meat again"`.
- Check whether `StoreAIContextAsync` correctly updates the meal preference or keeps the old value.

**Persist preferences to a file**
- Replace the in-memory `_preferences` field with `File.WriteAllText` / `File.ReadAllText` using `JsonSerializer`.
- Run the program, express a new preference, stop it, then run it again — preferences should survive the restart.

**Try with a different user ID**
- Change `"user123"` to `"user456"` when constructing `TravelPreferencesMemory`.
- Notice the agent starts with no knowledge — each user ID is isolated, which is the foundation of multi-tenant personalisation.