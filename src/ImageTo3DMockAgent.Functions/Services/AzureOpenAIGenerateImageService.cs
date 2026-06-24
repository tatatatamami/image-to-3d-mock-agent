using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageTo3DMockAgent.Functions.Models;
using ImageTo3DMockAgent.Functions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Images;

namespace ImageTo3DMockAgent.Functions.Services;

/// <summary>
/// Azure OpenAI（gpt-image-2）を使って画像を生成し、Blob Storage に保存するサービス。
///
/// 参考画像（referenceImageUrl）がある場合は image edit API を試みる。
/// edit API が利用不可の場合は、参考画像への言及をプロンプトに組み込んだ
/// テキスト生成にフォールバックする。
/// </summary>
public sealed class AzureOpenAIGenerateImageService(
    IOptions<AzureOpenAIOptions> openAIOptions,
    IOptions<BlobStorageOptions> blobOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<AzureOpenAIGenerateImageService> logger) : IGenerateImageService
{
    public async Task<GenerateImageResponse> GenerateAsync(
        GenerateImageRequest request,
        CancellationToken cancellationToken)
    {
        var opts = openAIOptions.Value;

        AzureOpenAIClient client = string.IsNullOrWhiteSpace(opts.ApiKey)
            ? new AzureOpenAIClient(new Uri(opts.Endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(opts.Endpoint), new AzureKeyCredential(opts.ApiKey));

        var imageClient = client.GetImageClient(opts.ImageDeployment);

        logger.LogInformation(
            "Generating image: deployment={Deployment} hasReferenceImage={HasRef}",
            opts.ImageDeployment,
            request.ReferenceImageUrl is not null);

        byte[] imageBytes;

        if (!string.IsNullOrWhiteSpace(request.ReferenceImageUrl))
        {
            imageBytes = await GenerateWithReferenceAsync(
                imageClient, request, cancellationToken);
        }
        else
        {
            imageBytes = await GenerateFromTextAsync(imageClient, request, cancellationToken);
        }

        var blobPath = BuildImageBlobPath();
        var imageUrl = await UploadToBlobAsync(imageBytes, blobPath, cancellationToken);

        logger.LogInformation("Image uploaded: {ImageUrl}", imageUrl);
        return new GenerateImageResponse(imageUrl, blobPath);
    }

    // -------------------------------------------------------------------

    private async Task<byte[]> GenerateWithReferenceAsync(
        ImageClient imageClient,
        GenerateImageRequest request,
        CancellationToken cancellationToken)
    {
        // まず edit API を試みる（DALL-E 2 / gpt-image-2 対応デプロイで使用可能）
        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var refBytes = await httpClient.GetByteArrayAsync(request.ReferenceImageUrl, cancellationToken);

            using var refStream = new MemoryStream(refBytes);
            var result = await imageClient.GenerateImageEditAsync(
                refStream,
                "reference.png",
                request.Prompt!,
                new ImageEditOptions { Size = ParseImageSize(request.Size) });

            // gpt-image-2 は常に base64（ImageBytes）を返す。ImageUri は返さない。
            if (result.Value.ImageBytes is { } editBytes)
                return editBytes.ToArray();

            // フォールバック: URI が返された場合（他のモデルとの互換性）
            if (result.Value.ImageUri is { } editUri)
                return await DownloadImageAsync(editUri, cancellationToken);

            throw new InvalidOperationException(
                "Image edit API returned neither bytes nor a URI.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Image edit API failed (deployment may not support it); falling back to text generation");
        }

        // フォールバック: 参考画像の URL をプロンプトに補足してテキスト生成
        var fallbackPrompt = $"{request.Prompt}\n\n" +
                             $"（スタイル参考: {request.ReferenceImageUrl} と同様のデザイン・色調で）";
        var fallbackRequest = new GenerateImageRequest
        {
            Prompt = fallbackPrompt,
            Size = request.Size,
        };
        return await GenerateFromTextAsync(imageClient, fallbackRequest, cancellationToken);
    }

    private async Task<byte[]> GenerateFromTextAsync(
        ImageClient imageClient,
        GenerateImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await imageClient.GenerateImageAsync(
            request.Prompt!,
            new ImageGenerationOptions
            {
                Size = ParseImageSize(request.Size),
                // ResponseFormat は指定しない（gpt-image-2 は未サポート）
                // モデルのデフォルトに委ねる: bytes があれば使用、なければ URI からダウンロード
            });

        if (result.Value.ImageBytes is { } bytes)
            return bytes.ToArray();

        return await DownloadImageAsync(result.Value.ImageUri!, cancellationToken);
    }

    private async Task<byte[]> DownloadImageAsync(Uri uri, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        return await httpClient.GetByteArrayAsync(uri, cancellationToken);
    }

    private async Task<string> UploadToBlobAsync(
        byte[] data,
        string blobPath,
        CancellationToken cancellationToken)
    {
        var opts = blobOptions.Value;
        var containerClient = new BlobContainerClient(opts.ConnectionString, opts.ContainerName);

        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.Blob,
            cancellationToken: cancellationToken);
        // ローカル開発: 既存コンテナが None だった場合も上書きして公開アクセスを保証
        await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobPath);
        using var stream = new MemoryStream(data, writable: false);
        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = "image/png" },
            cancellationToken: cancellationToken);

        return blobClient.Uri.ToString();
    }

    private static string BuildImageBlobPath()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        return $"images/generated-{timestamp}-{guid}.png";
    }

    private static GeneratedImageSize ParseImageSize(string size) => size switch
    {
        "1024x1792" => GeneratedImageSize.W1024xH1792,
        "1792x1024" => GeneratedImageSize.W1792xH1024,
        _ => GeneratedImageSize.W1024xH1024,
    };
}
