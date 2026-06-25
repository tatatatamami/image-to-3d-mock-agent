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
    // BlobContainerClient はスレッドセーフなため Singleton で安全にキャッシュ可能
    private readonly BlobContainerClient containerClient = new(options.Value.ConnectionString, options.Value.ContainerName);
    private volatile bool containerReady;

    public async Task<(string Url, string BlobPath)> UploadAsync(
        byte[] data,
        string blobPath,
        string contentType,
        CancellationToken cancellationToken)
    {
        // 初回アップロード時のみコンテナを作成（CreateIfNotExistsAsync は冪等）
        if (!containerReady)
        {
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            containerReady = true;
        }

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
