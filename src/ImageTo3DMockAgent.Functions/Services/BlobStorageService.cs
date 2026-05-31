using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageTo3DMockAgent.Functions.Exceptions;
using ImageTo3DMockAgent.Functions.Models;
using ImageTo3DMockAgent.Functions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ImageTo3DMockAgent.Functions.Services;

public sealed class BlobStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BlobUploadResult> UploadImageAsync(byte[] imageBytes, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        var options = GetOptions();
        var blobName = $"images/{createdAt:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.png";

        try
        {
            var blobServiceClient = new BlobServiceClient(options.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(options.ContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);
            await using var stream = new MemoryStream(imageBytes, writable: false);

            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = "image/png"
                    }
                },
                cancellationToken);

            _logger.LogInformation("Uploaded generated image to blob path {BlobPath}", blobName);

            return new BlobUploadResult
            {
                BlobPath = blobName,
                BlobUrl = blobClient.Uri.ToString()
            };
        }
        catch (Exception ex) when (ex is RequestFailedException or ArgumentException or FormatException)
        {
            _logger.LogError(ex, "Uploading generated image to Blob Storage failed for blob path {BlobPath}", blobName);
            throw new ServiceOperationException(HttpStatusCode.InternalServerError, "blob_upload_failed", "Blob Storage upload failed.", ex);
        }
    }

    private BlobStorageOptions GetOptions()
    {
        var options = new BlobStorageOptions
        {
            ConnectionString = _configuration["AZURE_STORAGE_CONNECTION_STRING"] ?? string.Empty,
            ContainerName = _configuration["AZURE_STORAGE_CONTAINER_NAME"] ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(options.ConnectionString) || string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new ServiceOperationException(HttpStatusCode.InternalServerError, "configuration_error", "Blob Storage configuration is missing or incomplete.");
        }

        return options;
    }
}
