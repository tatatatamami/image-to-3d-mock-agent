namespace ImageTo3DMockAgent.Functions.Options;

public sealed class AzureOpenAIOptions
{
    public const string ApiVersion = "2024-10-21";

    public string Endpoint { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string ImageDeployment { get; init; } = string.Empty;
}
