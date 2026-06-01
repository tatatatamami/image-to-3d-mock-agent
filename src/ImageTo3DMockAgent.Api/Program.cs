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
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection(BlobStorageOptions.SectionName));
builder.Services.Configure<TrellisOptions>(builder.Configuration.GetSection(TrellisOptions.SectionName));

// IMAGE_TO_3D_API_ENDPOINT が設定されていれば実サービスを使用し、未設定ならスタブを使用する
var trellisEndpoint = builder.Configuration["IMAGE_TO_3D_API_ENDPOINT"]
    ?? builder.Configuration[$"{TrellisOptions.SectionName}:ApiEndpoint"];

if (!string.IsNullOrWhiteSpace(trellisEndpoint))
{
    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
    builder.Services.AddHttpClient<TrellisGenerate3DAssetService>(client =>
    {
        client.BaseAddress = new Uri(trellisEndpoint);
        var apiKey = builder.Configuration["IMAGE_TO_3D_API_KEY"]
            ?? builder.Configuration[$"{TrellisOptions.SectionName}:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    });
    builder.Services.AddSingleton<IGenerate3DAssetService, TrellisGenerate3DAssetService>();
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
