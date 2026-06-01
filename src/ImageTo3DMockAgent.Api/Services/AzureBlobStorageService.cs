using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageTo3DMockAgent.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageTo3DMockAgent.Api.Services;

public sealed class AzureBlobStorageService(
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobStorageService> logger) : IBlobStorageService
{
    private readonly BlobStorageOptions blobOptions = options.Value;

    public async Task<(string Url, string BlobPath)> UploadAsync(
        byte[] data,
        string blobPath,
        string contentType,
        CancellationToken cancellationToken)
    {
        var containerClient = new BlobContainerClient(
            blobOptions.ConnectionString,
            blobOptions.ContainerName);

        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobPath);

        logger.LogInformation("Uploading {Bytes} bytes to blob {BlobPath}", data.Length, blobPath);

        using var stream = new MemoryStream(data, writable: false);
        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        var url = blobClient.Uri.ToString();
        logger.LogInformation("Upload complete: {Url}", url);

        return (url, blobPath);
    }
}
