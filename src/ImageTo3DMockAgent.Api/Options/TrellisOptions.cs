using System.ComponentModel.DataAnnotations;

namespace ImageTo3DMockAgent.Api.Options;

public sealed class TrellisOptions
{
    public const string SectionName = "Trellis";

    /// <summary>TRELLIS FastAPI サーバーのベース URL（例: http://localhost:8080）</summary>
    [Required]
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>API キー（Authorization: Bearer ヘッダーに付与）。省略可。</summary>
    public string? ApiKey { get; set; }

    /// <summary>元画像を参照するための Blob Storage ベース URL。</summary>
    [Required]
    public string BlobBaseUrl { get; set; } = string.Empty;
}
