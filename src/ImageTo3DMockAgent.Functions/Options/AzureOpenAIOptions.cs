namespace ImageTo3DMockAgent.Functions.Options;

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    /// <summary>Azure AI Foundry / Azure OpenAI エンドポイント（例: https://xxx.services.ai.azure.com/）</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>画像生成モデルのデプロイ名（例: gpt-image-2）</summary>
    public string ImageDeployment { get; set; } = "gpt-image-2";

    /// <summary>API キー（省略時は DefaultAzureCredential を使用）</summary>
    public string? ApiKey { get; set; }
}
