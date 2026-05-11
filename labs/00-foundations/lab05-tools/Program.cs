// Lab 05: Tools and Function Calling
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
var b = Kernel.CreateBuilder();
b.AddAzureOpenAIChatCompletion(Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")!, Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!, Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!);
b.Plugins.AddFromType<TimePlugin>();
var k = b.Build();
var s = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
while (true) { Console.Write("User > "); var i = Console.ReadLine(); if (string.IsNullOrWhiteSpace(i)) break; var r = await k.InvokePromptAsync(i, new(s)); Console.WriteLine($"Assistant > {r}"); }
public record ToolResult(string Result);
[JsonSerializable(typeof(ToolResult))]
internal partial class ToolJsonSerializerContext : JsonSerializerContext {}
public class TimePlugin { [KernelFunction] public string GetCurrentTime() => DateTime.Now.ToString("F"); }
