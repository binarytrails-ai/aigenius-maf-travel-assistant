// Lab 05: Tools and Function Calling
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")!,
    endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
    apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!
);

builder.Plugins.AddFromType<TimePlugin>();

var kernel = builder.Build();

var settings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

while (true)
{
    Console.Write("User > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    var result = await kernel.InvokePromptAsync(input, new(settings));
    Console.WriteLine($"Assistant > {result}");
}

public record ToolResult(string Result);

[JsonSerializable(typeof(ToolResult))]
internal partial class ToolJsonSerializerContext : JsonSerializerContext
{
}

public class TimePlugin
{
    [KernelFunction]
    public string GetCurrentTime() => DateTime.Now.ToString("F");
}
