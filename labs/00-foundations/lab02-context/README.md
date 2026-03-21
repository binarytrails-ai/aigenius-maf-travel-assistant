# Lab 02: Travel Assistant with Memory

In this lab, you will enhance your travel assistant by providing additional context.
By the end of this lab, you will:

- Understand how to use `AIContextProvider` to provide context to the agent

## Key Implementation Details

### What is a Context Provider?

When an agent receives a question, the only information it has by default is its system prompt and the current conversation history. An **`AIContextProvider`** is a hook that runs **before each agent invocation** to inject additional information into that call — without permanently changing the agent's configuration.

Think of it like a research assistant who pulls the relevant files off the shelf before every meeting. The agent gets the right background knowledge just in time, without having to store everything in its main instructions.

### How `AIContextProvider` Works

The provider has a two-phase lifecycle:

- **Before the call** (`ProvideAIContextAsync`) — returns an `AIContext` object containing extra instructions, messages, or tools to include in this specific invocation
- **After the call** (optional, `InvokedAsync`) — can process the result, which is useful for learning or state management

In this lab, `TravelKnowledgeContext` overrides `ProvideAIContextAsync` to return a hard-coded string of destination knowledge:

```csharp
protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
{
    return new ValueTask<AIContext>(new AIContext
    {
        Instructions = "Use the following travel knowledge when answering questions:\n\n" + TravelKnowledge
    });
}
```

The `AIContext.Instructions` text is merged with the agent's base instructions on every call. The model sees a richer prompt without you needing to paste the knowledge into the agent's permanent `Instructions` field.

### Registering the Provider with the Agent

The context provider is attached at agent creation time via the `AIContextProviders` list in `ChatClientAgentOptions`:

```csharp
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [travelContext]
});
```

You can register multiple providers — they run in order and each one can build on the context returned by the previous one. This makes it easy to compose independent sources of knowledge (e.g. one for travel knowledge, one for user preferences, etc).

### Why Not Just Put Everything in the System Prompt?

Hard-coding all knowledge into the agent's `Instructions` has real limitations:

- **Token cost** — every call includes the full prompt, even when most of it is irrelevant
- **Stale knowledge** — a static string cannot be updated without redeploying
- **No personalisation** — you cannot tailor context to the specific user or query

Context providers solve this by making context **dynamic and per-call**. In later labs you will see providers that look up information from a database or vector search index based on what the user actually asked.

## Instructions

### Step 1: Navigate to the Lab Folder

```bash
cd labs/00-foundations/lab02-context
```

### Step 2: Run the Program

With .NET 10's file-based apps, you can run the single .cs file directly:

```bash
dotnet run Program.cs
```

Or in Visual Studio Code, open Program.cs and click the **"Run"** button that appears above the code.

### Step 3: Observe the Output

You should see the agent provide responses that incorporate the additional context you provided. Notice how the agent can use the context to give more relevant answers to your queries.

---

## Challenges and Next Steps

**Extend the destination knowledge**
- Add a new destination to the `TravelKnowledge` constant (e.g. Italy, New Zealand, or Mexico) and try asking questions about it.
- Notice how the agent now answers accurately even though the model may not have had this specific data.

**Try queries that fall outside the context**
- Ask about a destination that is NOT in the knowledge block, such as `"What should I see in Brazil?"`.
- Observe how the agent falls back to its general training data — the context provider only supplements, it does not restrict.

**Make the context query-aware**
- Modify `ProvideAIContextAsync` to inspect the user's message via `context.AIContext` and return only the relevant destination section instead of the full knowledge block. This reduces token usage and is closer to how a real RAG retrieval step works.

**Add a second context provider**
- Create a second class (e.g. `UserPreferencesContext`) that returns a budget preference or dietary restriction as additional instructions.
- Register both providers in `AIContextProviders = [travelContext, userPrefsContext]` and ask a question that requires both sources.