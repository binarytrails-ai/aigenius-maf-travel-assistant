# Lab 03: Tools and Function Calling

In this lab, you will create an AI-powered travel assistant that can use tools to perform actions.

You will implement an agent with tools: date calculations and time zone calculations for Australian cities. The agent will automatically decide which tool to use based on the user's question.

By the end of this lab, you will:

- ✅ Create tools (functions) that agents can call automatically
- ✅ Understand how agents decide when and which tools to use
- ✅ See parameter extraction from natural language in action

## Key Implementation Details

### What is Function Calling?

Without tools, an agent can only answer from its training knowledge. With tools, it can take **actions** — look up live data, call APIs, read files, perform calculations.

**Function calling** (also called tool use) works like this:

1. You register functions as tools on the agent
2. The model reads each tool's name, description, and parameter descriptions
3. When the user asks a question, the model decides whether a tool would help
4. If yes, it outputs a structured tool call (name + arguments) instead of a text response
5. The framework executes the function and sends the result back to the model
6. The model uses the result to write its final response

The agent handles steps 4-6 automatically — you only define the functions.

### Defining Tools with `AIFunctionFactory.Create`

Any static or instance method can be turned into an agent tool with a single line:

```csharp
AIFunctionFactory.Create(CalculateTimeZone)
```

The framework uses reflection to read the method signature and builds a tool description automatically. To make the tool's purpose clear to the model, use:

- **`[Description(...)]` on the method** — tells the model *when* to use this tool
- **`[Description(...)]` on each parameter** — tells the model what to pass in

```csharp
[Description("Calculate the time difference between two Australian cities and optionally convert a specific time.")]
static string CalculateTimeZone(
    [Description("Origin Australian city (e.g., 'Sydney', 'Melbourne', 'Brisbane')")] string fromCity,
    [Description("Destination Australian city (e.g., 'Perth', 'Adelaide', 'Darwin')")] string toCity,
    [Description("Optional: time in origin city in 24-hour format HH:mm")] string? localTime = null)

### Registering Tools on the Agent

Tools are passed in via `ChatOptions.Tools` when creating the agent:

```csharp
var tools = new List<AITool>
{
    AIFunctionFactory.Create(GetCurrentDate),
    AIFunctionFactory.Create(CalculateDateDifference),
    AIFunctionFactory.Create(CalculateDaysUntil),
    AIFunctionFactory.Create(CalculateTimeZone)
};

var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new() { Tools = tools }
});
```

The model can call multiple tools in a single turn if needed. For example, `"How many days until March 30th, and what time will it be in Perth when it's 2 PM in Sydney on that day?"` might trigger `CalculateDaysUntil` and `CalculateTimeZone` together.

### Tool Return Values and JSON

All four tools return a **JSON string**. This is the recommended approach because:

- JSON is structured, so the model can reliably extract individual fields
- Returning multiple values (e.g. flight number, price, departure time) in one call is straightforward
- It mirrors what a real REST API would return

---

## Instructions

### Step 1: Navigate to the Lab Folder

```bash
cd labs/00-foundations/lab05-tools
```

### Step 2: Run the Program

With .NET 10's file-based apps, you can run the single .cs file directly:

```bash
dotnet run Program.cs
```

Or in Visual Studio Code, open Program.cs and click the **"Run"** button that appears above the code

### Step 3: Observe the Output

You should see the agent automatically call tools to answer the questions. Notice how:

- The agent decides which tool(s) to call — you never tell it explicitly
- Parameters like city names and times are extracted directly from natural language
- The agent weaves the tool results into a natural conversational response
- The agent can chain multiple tools in a single turn when needed
- Optional parameters (like `localTime`) are handled automatically

## Sample Prompts to Try

After running the program, try these prompts to explore the tool capabilities:

**Time Zone Calculations:**

- "What's the time difference between Sydney and Perth?"
- "If it's 10:00 AM in Brisbane, what time is it in Adelaide?"
- "When it's 5:30 PM in Melbourne, what time is it in Darwin?"
- "I need to call someone in Hobart at 2:00 PM their time. What time is that in Perth?"

**Date Calculations:**

- "How many days until April 15th?"
- "Calculate the days between March 25th and May 10th"
- "What day of the week is today?"

**Multiple Tools:**

- "How many days until April 1st, and what time will it be in Perth when it's 9:00 AM in Sydney on that day?"

---

## Challenges and Next Steps


**Add a new tool**

- Write a `CheckVisaRequirements` function that reads the visa policy markdown files in the `data/` folder.
- Use `AIFunctionFactory.Create(CheckVisaRequirements)` and ask: `"Do I need a visa to travel from USA to Japan?"`.

**Test with unknown cities**

- Ask: `"What's the time difference between Sydney and Auckland?"` (Auckland is in New Zealand, not Australia).
- Does the agent gracefully handle cities not in the time zone dictionary?