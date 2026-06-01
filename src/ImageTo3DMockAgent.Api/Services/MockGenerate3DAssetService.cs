using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ImageTo3DMockAgent.Api.Models;
using ImageTo3DMockAgent.Api.Options;
using Microsoft.Extensions.Options;

namespace ImageTo3DMockAgent.Api.Services;

public sealed partial class MockGenerate3DAssetService(IOptions<MockAssetStorageOptions> options) : IGenerate3DAssetService
{
    private readonly MockAssetStorageOptions storageOptions = options.Value;

    public Task<Generate3DResponse> GenerateAsync(Generate3DRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sourceAssetPath = !string.IsNullOrWhiteSpace(request.ImageBlobPath)
            ? request.ImageBlobPath!.Trim()
            : GetPathFromUrl(request.ImageUrl!);

        var sourceImageUrl = !string.IsNullOrWhiteSpace(request.ImageUrl)
            ? request.ImageUrl!.Trim()
            : CombineUrl(storageOptions.SourceImageBaseUrl, sourceAssetPath);

        var fileStem = GetSafeFileStem(sourceAssetPath);
        var outputFormat = request.GetOutputFormatOrDefault();
        var modelBlobPath = $"models/{fileStem}.{outputFormat}";
        var modelUrl = CombineUrl(storageOptions.ModelBaseUrl, modelBlobPath);

        return Task.FromResult(new Generate3DResponse(modelUrl, modelBlobPath, sourceImageUrl));
    }

    private static string GetPathFromUrl(string imageUrl)
    {
        var uri = new Uri(imageUrl, UriKind.Absolute);
        return uri.AbsolutePath.TrimStart('/');
    }

    private static string GetSafeFileStem(string sourceAssetPath)
    {
        var rawFileName = Path.GetFileNameWithoutExtension(Uri.UnescapeDataString(sourceAssetPath));
        var sanitized = InvalidFileNameCharactersRegex().Replace(rawFileName, "-").Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? $"model-{ComputeHash(sourceAssetPath)}" : sanitized;
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes[..4]).ToLowerInvariant();
    }

    private static string CombineUrl(string baseUrl, string relativePath)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/') + "/";
        var normalizedPath = relativePath.TrimStart('/');
        return new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), normalizedPath).ToString();
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_-]+", RegexOptions.Compiled)]
    private static partial Regex InvalidFileNameCharactersRegex();
}
