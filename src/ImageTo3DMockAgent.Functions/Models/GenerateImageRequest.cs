namespace ImageTo3DMockAgent.Functions.Models;

public sealed class GenerateImageRequest
{
    public string? Prompt { get; init; }

    public string? Size { get; init; }

    public string? Quality { get; init; }

    public int? N { get; init; }
}
