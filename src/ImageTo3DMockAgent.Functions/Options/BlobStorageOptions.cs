namespace ImageTo3DMockAgent.Functions.Options;

public sealed class BlobStorageOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public string ContainerName { get; init; } = string.Empty;
}
