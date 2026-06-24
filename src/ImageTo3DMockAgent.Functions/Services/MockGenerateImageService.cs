using ImageTo3DMockAgent.Functions.Models;

namespace ImageTo3DMockAgent.Functions.Services;

/// <summary>
/// AZURE_OPENAI_ENDPOINT 未設定時のスタブ実装。
/// 実際の画像生成は行わず、ダミーの URL を返す。
/// </summary>
public sealed class MockGenerateImageService : IGenerateImageService
{
    private const string MockImageUrl =
        "https://mockstorage.local/images/mock-generated.png";

    private const string MockBlobPath = "images/mock-generated.png";

    public Task<GenerateImageResponse> GenerateAsync(
        GenerateImageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new GenerateImageResponse(MockImageUrl, MockBlobPath));
    }
}
