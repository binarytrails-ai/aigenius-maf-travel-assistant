# Lab 08: Hosting Agents as Web Services

In this lab, you will learn how to host an AI agent as a web service using ASP.NET Core, making it accessible via HTTP endpoints.

You will create a web API that exposes your agent through OpenAI-compatible endpoints and an interactive web interface, allowing it to be consumed by various clients.

By the end of this lab, you will:

- ✅ Host an agent as an ASP.NET Core web service
- ✅ Expose agents via OpenAI-compatible API endpoints
- ✅ Call your agent from HTTP clients using the OpenAI API contract

## Key Implementation Details

### From Console App to Web Service

Previous labs ran agents as console applications. Hosting an agent as a service lets any HTTP client — a browser, a mobile app, another service — interact with the same agent.

The key difference is replacing `WebApplication.CreateBuilder` for a standard ASP.NET Core host instead of running the agent inline:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register services (OpenTelemetry, logging, CORS, AGUI)
builder.Services.AddAGUI();

var app = builder.Build();

// Map endpoints
app.MapOpenAIChatCompletions(travelAgent);
app.MapAGUI("/agent/travel", travelAgent);

await app.RunAsync();
```

The method `MapOpenAIChatCompletions` exposes an OpenAI-compatible REST API endpoint.

## Instructions

### Step 1: Navigate to the Lab Folder

```bash
cd labs/00-foundations/lab08-host
```

### Step 2: Run the Web Service

```bash
dotnet run
```

You should see output indicating the service has started with the available endpoints:

```
HOST STARTED - Agent is now available at the following endpoints:
  OpenAI API:   http://localhost:5000/TravelAssistant/v1/chat/completions
Press Ctrl+C to shut down
```

### Step 3: Test the Agent via the HTTP File

Open `TravelAssistant.http` in VS Code and click **Send Request** next to any of the pre-built requests to call the OpenAI-compatible endpoint directly.

### Step 4: Stop the Service

Press `Ctrl+C` in the terminal to shut down the web service.

