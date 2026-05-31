namespace ImageTo3DMockAgent.Functions.Models;

public sealed class ImageGenerationResult
{
    public required byte[] ImageBytes { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
