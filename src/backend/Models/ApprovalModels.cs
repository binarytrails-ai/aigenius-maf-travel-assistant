using System.Text.Json.Serialization;

namespace ContosoTravelAgent.Host.Models;

// Define approval models
public sealed class ApprovalRequest
{
    [JsonPropertyName("approval_id")]
    public required string ApprovalId { get; init; }

    [JsonPropertyName("function_name")]
    public required string FunctionName { get; init; }

    [JsonPropertyName("function_arguments")]
    public IDictionary<string, object?>? FunctionArguments { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

public sealed class ApprovalResponse
{
    [JsonPropertyName("approval_id")]
    public required string ApprovalId { get; init; }

    [JsonPropertyName("approved")]
    public required bool Approved { get; init; }
}

[JsonSerializable(typeof(ApprovalRequest))]
[JsonSerializable(typeof(ApprovalResponse))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
public sealed partial class ApprovalJsonContext : JsonSerializerContext;

#pragma warning restore CS1591
