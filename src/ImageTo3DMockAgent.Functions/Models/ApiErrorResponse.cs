namespace ImageTo3DMockAgent.Functions.Models;

public sealed class ApiErrorResponse
{
    public string Error { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string[]>? Details { get; init; }
}
