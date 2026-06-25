using Azure.Monitor.OpenTelemetry.Exporter;
using ImageTo3DMockAgent.Api.Options;
using ImageTo3DMockAgent.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.Configure<MockAssetStorageOptions>(builder.Configuration.GetSection(MockAssetStorageOptions.SectionName));

// IMAGE_TO_3D_API_ENDPOINT が設定されていれば実サービスを使用し、未設定ならスタブを使用する
var trellisEndpoint = builder.Configuration["IMAGE_TO_3D_API_ENDPOINT"]
    ?? builder.Configuration[$"{TrellisOptions.SectionName}:ApiEndpoint"];

if (!string.IsNullOrWhiteSpace(trellisEndpoint))
{
    // TRELLIS モード: 起動時に必須オプションを検証（設定漏れを即座に検出）
    builder.Services.AddOptions<BlobStorageOptions>()
        .BindConfiguration(BlobStorageOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddOptions<TrellisOptions>()
        .BindConfiguration(TrellisOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
    builder.Services.AddHttpClient<TrellisGenerate3DAssetService>(client =>
    {
        // 末尾スラッシュを保証することで相対 URL 解決が正しく動作する
        client.BaseAddress = new Uri(trellisEndpoint.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromMinutes(10); // TRELLIS の 3D 生成は数分かかる場合がある
        var apiKey = builder.Configuration["IMAGE_TO_3D_API_KEY"]
            ?? builder.Configuration[$"{TrellisOptions.SectionName}:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    });
    // AddSingleton ではなく AddTransient を使う
    // AddHttpClient<T> が登録する typed client (Transient) を利用するため
    builder.Services.AddTransient<IGenerate3DAssetService>(
        sp => sp.GetRequiredService<TrellisGenerate3DAssetService>());
}
else
{
    builder.Services.AddSingleton<IGenerate3DAssetService, MockGenerate3DAssetService>();
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
