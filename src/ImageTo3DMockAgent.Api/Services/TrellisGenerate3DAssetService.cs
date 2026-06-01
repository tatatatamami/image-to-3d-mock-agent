using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ImageTo3DMockAgent.Api.Models;
using ImageTo3DMockAgent.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageTo3DMockAgent.Api.Services;

/// <summary>
/// ローカルまたはリモートの TRELLIS FastAPI サーバーを呼び出して 3D アセットを生成するサービス。
/// </summary>
public sealed class TrellisGenerate3DAssetService(
    HttpClient httpClient,
    IBlobStorageService blobStorageService,
    IOptions<TrellisOptions> options,
    ILogger<TrellisGenerate3DAssetService> logger) : IGenerate3DAssetService
{
    private static readonly Dictionary<string, string> ContentTypeByFormat = new()
    {
        ["glb"] = "model/gltf-binary",
        ["obj"] = "application/octet-stream",
    };

    public async Task<Generate3DResponse> GenerateAsync(Generate3DRequest request, CancellationToken cancellationToken)
    {
        var outputFormat = request.GetOutputFormatOrDefault();
        var quality = request.GetQualityOrDefault();

        // imageUrl を解決する（imageBlobPath のみの場合は Blob URL を構築）
        var imageUrl = ResolveImageUrl(request);

        logger.LogInformation(
            "Calling TRELLIS API: imageUrl={ImageUrl} format={Format} quality={Quality}",
            imageUrl, outputFormat, quality);

        // TRELLIS API へリクエスト
        var trellisRequest = new TrellisApiRequest(imageUrl, outputFormat, quality);
        byte[] assetBytes;
        try
        {
            var response = await httpClient.PostAsJsonAsync("/generate-3d", trellisRequest, cancellationToken);
            response.EnsureSuccessStatusCode();
            assetBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "TRELLIS API call failed");
            throw new InvalidOperationException("Image-to-3D conversion failed.", ex);
        }

        logger.LogInformation("TRELLIS returned {Bytes} bytes for format={Format}", assetBytes.Length, outputFormat);

        // Blob Storage にアップロード
        var blobPath = BuildModelBlobPath(outputFormat);
        var contentType = ContentTypeByFormat.GetValueOrDefault(outputFormat, "application/octet-stream");

        string modelUrl;
        string modelBlobPath;
        try
        {
            (modelUrl, modelBlobPath) = await blobStorageService.UploadAsync(
                assetBytes, blobPath, contentType, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Blob Storage upload failed for path={BlobPath}", blobPath);
            throw new InvalidOperationException("Failed to upload 3D asset to Blob Storage.", ex);
        }

        // 元画像の URL を解決
        var sourceImageUrl = !string.IsNullOrWhiteSpace(request.ImageUrl)
            ? request.ImageUrl!.Trim()
            : BuildSourceImageUrl(request.ImageBlobPath!);

        return new Generate3DResponse(modelUrl, modelBlobPath, sourceImageUrl);
    }

    private string ResolveImageUrl(Generate3DRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return request.ImageUrl!.Trim();
        }

        // imageBlobPath のみの場合: Blob Storage の公開 URL を構築
        return BuildSourceImageUrl(request.ImageBlobPath!.Trim());
    }

    private string BuildSourceImageUrl(string blobPath)
    {
        var baseUrl = options.Value.BlobBaseUrl.TrimEnd('/');
        var normalizedPath = blobPath.TrimStart('/');
        return $"{baseUrl}/{normalizedPath}";
    }

    private static string BuildModelBlobPath(string outputFormat)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        return $"models/{timestamp}-{guid}.{outputFormat}";
    }

    // TRELLIS API へ送るリクエスト DTO
    private sealed record TrellisApiRequest(
        [property: JsonPropertyName("imageUrl")] string ImageUrl,
        [property: JsonPropertyName("outputFormat")] string OutputFormat,
        [property: JsonPropertyName("quality")] string Quality);
}
