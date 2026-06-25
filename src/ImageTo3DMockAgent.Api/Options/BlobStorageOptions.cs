using System.ComponentModel.DataAnnotations;

namespace ImageTo3DMockAgent.Api.Options;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = string.Empty;
}
