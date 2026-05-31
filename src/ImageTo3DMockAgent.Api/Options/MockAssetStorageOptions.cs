namespace ImageTo3DMockAgent.Api.Options;

public sealed class MockAssetStorageOptions
{
    public const string SectionName = "MockAssetStorage";

    public string SourceImageBaseUrl { get; set; } = "https://mockstorage.local/assets";

    public string ModelBaseUrl { get; set; } = "https://mockstorage.local/assets";
}
