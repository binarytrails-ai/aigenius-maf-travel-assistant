using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")!,
    endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
    apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!
);

builder.Plugins.AddFromType<TimePlugin>();

Kernel kernel = builder.Build();

OpenAIPromptExecutionSettings settings = new() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

while (true)
{
    Console.Write("User > ");
    string? input = Console.ReadLine();
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
    [KernelFunction, System.ComponentModel.Description("Get the current time")]
    public string GetCurrentTime() => DateTime.Now.ToString("F");
}
