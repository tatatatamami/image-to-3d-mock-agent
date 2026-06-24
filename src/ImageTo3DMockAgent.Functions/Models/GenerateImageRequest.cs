namespace ImageTo3DMockAgent.Functions.Models;

public sealed class GenerateImageRequest
{
    /// <summary>生成したい画像の自然言語説明</summary>
    public string? Prompt { get; init; }

    /// <summary>スタイル参照用の画像 URL（任意）。http/https 絶対 URL。</summary>
    public string? ReferenceImageUrl { get; init; }

    /// <summary>出力画像サイズ。1024x1024 / 1024x1792 / 1792x1024</summary>
    public string Size { get; init; } = "1024x1024";
}
