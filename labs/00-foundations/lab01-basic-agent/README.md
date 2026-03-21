# Lab 01: Basic Agent

In this lab, you will create your first AI-powered travel assistant using the Microsoft Agent Framework.

You will implement a basic agent that can have multi-turn conversations and provide travel recommendations.

By the end of this lab, you will:

- ✅ Create and configure an AI agent using the Microsoft Agent Framework
- ✅ Connect your agent to Azure OpenAI or GitHub Models
- ✅ Implement multi-turn conversations with session management
- ✅ Understand the basics of agent instructions and chat configuration

## Key Implementation Details

### The Microsoft Agent Framework

This lab uses the **Microsoft Agent Framework** (`Microsoft.Agents.AI`), a production-ready .NET library for building AI-powered agents. It builds on top of `Microsoft.Extensions.AI`, which provides a common `IChatClient` interface that works with many different AI providers — meaning you can swap your AI backend without rewriting your agent logic.

### Creating the Chat Client (`IChatClient`)

The first step is creating a chat client that connects to an AI model. The code checks for credentials in this order:

- **Azure OpenAI** — uses `AZURE_AI_SERVICES_ENDPOINT` and `AZURE_AI_SERVICES_KEY` from your `.env` file
- **GitHub Models** — falls back to `GITHUB_TOKEN` if Azure credentials are not set

Both providers use `AzureOpenAIClient` under the hood because GitHub Models exposes an OpenAI-compatible API endpoint. The resulting client implements `IChatClient`, which is the standard .NET AI abstraction.

> Want to try a different provider? The `IChatClient` interface is supported by many services. You can connect to standard OpenAI, Ollama (local models), or Anthropic via Azure AI Foundry. See the [Microsoft Agent Framework agent types documentation](https://learn.microsoft.com/agent-framework/agents/) for the full list.

### Turning the Chat Client into an Agent (`.AsAIAgent()`)

The `.AsAIAgent()` extension method wraps your `IChatClient` in a `ChatClientAgent`. This is what gives you agent-level capabilities on top of a plain chat client:

- **`Name`** — a label for the agent, used in logs and multi-agent scenarios
- **`Instructions`** — the system prompt that shapes the agent's personality and behaviour (e.g. "You are a helpful travel assistant...")
- **`Tools`** — a list of functions the agent can call. In this lab it is empty (`[]`), but later labs add real tools

An alternative, simpler way to create an agent directly without first building an `IChatClient` is:

```csharp
var agent = new ChatClientAgent(chatClient, instructions: "You are a helpful assistant");
```

### The Middleware Pipeline (`.AsBuilder()`)

After creating the agent, the code calls `.AsBuilder()` to attach middleware. Think of middleware as layers that wrap each agent call, adding cross-cutting behaviour:

- **`.UseOpenTelemetry()`** — records traces and spans so you can observe what the agent is doing
- **`.UseLogging()`** — writes structured log messages for each interaction

### Sessions and Multi-Turn Conversations

A **session** (`AgentSession`) represents a single conversation thread. By passing the same session to each `RunAsync()` call, the agent remembers what was said earlier:

```csharp
AgentSession session = await agent.CreateSessionAsync();
var response1 = await agent.RunAsync("What are some must-visit places in Australia?", session);
var response2 = await agent.RunAsync("Which one would you recommend for families with kids?", session);
```

Without a session, each call would be independent and the agent would have no memory of previous turns.

### Built-in Observability (OpenTelemetry)

The program sets up OpenTelemetry tracing and logging from the start. Traces are exported to an OTLP endpoint (default: `http://localhost:4317`), which is compatible with tools like the .NET Aspire Dashboard.

---

## Instructions

### Step 1: Navigate to the Lab Folder

```bash
cd labs/00-foundations/lab01-basic-agent
```

### Step 2: Run the Program

With .NET 10's file-based apps, you can run the single .cs file directly:

```bash
dotnet run Program.cs
```

Or in Visual Studio Code, open Program.cs and click the **"Run"** button that appears above the code.

---

## Challenges and Next Steps

Now that your basic agent is running, try these challenges to deepen your understanding:

**Modify the agent's personality**
- Change the `Instructions` string to make the agent respond only in bullet points.
- Make the agent more concise by adding "Keep your answers under 50 words" to the instructions.

**Test the multi-turn memory**
- Add a third `RunAsync()` call that refers back to the previous answers, for example: `"What is the best time of year to visit the place you recommended?"`
- Observe how the agent uses context from earlier in the conversation.

**Swap the AI model**
- If you have Azure credentials, try changing the `AZURE_TEXT_MODEL_NAME` in your `.env` to a different model (e.g. `gpt-4.1`, `gpt-4o-mini`) and compare response quality and speed.
- If you are using GitHub Models, change `GITHUB_TEXT_MODEL_ID` to `meta-llama-3.1-70b-instruct` or `mistral-small` to try open-source models.

**Explore the `ChatClientAgentOptions`**
- Add a `MaxOutputTokens` limit inside `ChatOptions` to cap response length.
- Add a `Temperature` value (0.0 = focused/deterministic, 1.0 = creative/varied) and observe the difference in responses.