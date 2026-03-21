# Lab 06: PII Filtering Middleware

In this lab, you will implement **PII (Personally Identifiable Information) filtering middleware** to redact sensitive information from conversations.

You will create middleware that filters both user inputs and agent outputs to protect privacy by redacting phone numbers, email addresses, and names.

By the end of this lab, you will:

- ✅ Understand how to implement agent-level middleware
- ✅ Protect sensitive information in both directions (input and output)

## Key Implementation Details

### What is Middleware in the Agent Pipeline?

Middleware is code that sits between the user's message and the agent's final response. Agent middleware wraps the execution pipeline so you can:

- Inspect or modify messages **before** they reach the model
- Inspect or modify responses **after** the model replies
- Add cross-cutting concerns such as content filtering, logging, rate limiting, or validation

### The Middleware Signature

Every middleware function follows this delegate signature:

```csharp
async Task<AgentResponse> PIIMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken)
```

### Registering Middleware with `.Use()`

Middleware is added to the agent builder using `.Use()` before calling `.Build()`:

```csharp
var agent = chatClient.AsAIAgent(...)
    .AsBuilder()
    .Use(PIIMiddleware, null)      // custom PII filtering middleware
    .UseOpenTelemetry(...)         // built-in telemetry middleware
    .UseLogging(loggerFactory)     // built-in logging middleware
    .Build();
```

Middleware executes in registration order. The first `.Use()` wraps the outermost layer, so the PII filter runs before OpenTelemetry sees message content — which means telemetry only ever records the redacted version.

### Two-Directional Filtering

PII filtering is applied both before and after the model call:

```csharp
// Before: filter what the model receives
var filteredMessages = FilterMessages(messages);
appLogger.LogInformation("PII Middleware - Filtering Messages Pre-Run");

var response = await innerAgent.RunAsync(filteredMessages, session, options, cancellationToken);

// After: filter what the user receives
response.Messages = FilterMessages(response.Messages);
appLogger.LogInformation("PII Middleware - Filtering Messages Post-Run");
```

This ensures sensitive data is never sent to the external model.

### PII Detection with Regular Expressions

```csharp
new(@"\b\d{3}-\d{3}-\d{4}\b", RegexOptions.Compiled)           // Phone: 123-456-7890
new(@"\b[\w\.-]+@[\w\.-]+\.\w+\b", RegexOptions.Compiled)      // Email: user@example.com
new(@"\b[A-Z][a-z]+\s[A-Z][a-z]+\b", RegexOptions.Compiled)   // Name: John Doe
```

These patterns are intentionally simple for demonstration purposes. Production systems should use ML-based detectors such as the [Azure AI Language PII detection API](https://learn.microsoft.com/azure/ai-services/language-service/personally-identifiable-information/overview) for higher accuracy and broader entity coverage.

## Instructions

### Step 1: Navigate to the Lab Folder

```bash
cd labs/00-foundations/lab06-middleware
```

### Step 3: Run the Program

With .NET 10's file-based apps, you can run the single .cs file directly:

```bash
dotnet run Program.cs
```

Or in Visual Studio Code, open Program.cs and click the **Run** button that appears above the code.

### Step 4: Observe the Output

The program identifies and redacts PII from the user input and agent responses.