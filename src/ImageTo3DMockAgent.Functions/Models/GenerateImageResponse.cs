namespace ImageTo3DMockAgent.Functions.Models;

public sealed class GenerateImageResponse
{
    public string ImageUrl { get; init; } = string.Empty;

    public string ImageBlobPath { get; init; } = string.Empty;

    public string Prompt { get; init; } = string.Empty;

    public string Size { get; init; } = string.Empty;

    public string Quality { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}
