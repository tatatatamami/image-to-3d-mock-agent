namespace ImageTo3DMockAgent.Api.Models;

public sealed class Generate3DRequest
{
    public string? ImageUrl { get; init; }

    public string? ImageBlobPath { get; init; }

    public string? OutputFormat { get; init; }

    public string? Quality { get; init; }

    public string GetOutputFormatOrDefault() => string.IsNullOrWhiteSpace(OutputFormat) ? "glb" : OutputFormat.Trim().ToLowerInvariant();

    public string GetQualityOrDefault() => string.IsNullOrWhiteSpace(Quality) ? "preview" : Quality.Trim().ToLowerInvariant();
}
