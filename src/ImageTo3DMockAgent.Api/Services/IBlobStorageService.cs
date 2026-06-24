namespace ImageTo3DMockAgent.Api.Services;

public interface IBlobStorageService
{
    Task<(string Url, string BlobPath)> UploadAsync(
        byte[] data,
        string blobPath,
        string contentType,
        CancellationToken cancellationToken);
}
