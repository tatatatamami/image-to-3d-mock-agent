using ImageTo3DMockAgent.Functions.Options;
using ImageTo3DMockAgent.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// local.settings.json のフラットキーとセクションキーの両方をサポート
var openAIEndpoint =
    builder.Configuration["AZURE_OPENAI_ENDPOINT"] ??
    builder.Configuration[$"{AzureOpenAIOptions.SectionName}:Endpoint"];

var openAIDeployment =
    builder.Configuration["AZURE_OPENAI_IMAGE_DEPLOYMENT"] ??
    builder.Configuration[$"{AzureOpenAIOptions.SectionName}:ImageDeployment"] ??
    "gpt-image-2";

var openAIApiKey =
    builder.Configuration["AZURE_OPENAI_API_KEY"] ??
    builder.Configuration[$"{AzureOpenAIOptions.SectionName}:ApiKey"];

var blobConnectionString =
    builder.Configuration["AZURE_STORAGE_CONNECTION_STRING"] ??
    builder.Configuration[$"{BlobStorageOptions.SectionName}:ConnectionString"] ??
    builder.Configuration["AzureWebJobsStorage"];

var blobContainerName =
    builder.Configuration["AZURE_STORAGE_CONTAINER_NAME"] ??
    builder.Configuration[$"{BlobStorageOptions.SectionName}:ContainerName"] ??
    "images";

builder.Services.Configure<AzureOpenAIOptions>(opts =>
{
    opts.Endpoint = openAIEndpoint ?? string.Empty;
    opts.ImageDeployment = openAIDeployment;
    opts.ApiKey = openAIApiKey;
});

builder.Services.Configure<BlobStorageOptions>(opts =>
{
    opts.ConnectionString = blobConnectionString ?? string.Empty;
    opts.ContainerName = blobContainerName;
});

builder.Services.AddHttpClient();

// AZURE_OPENAI_ENDPOINT が設定されていれば実サービス、未設定ならスタブ
if (!string.IsNullOrWhiteSpace(openAIEndpoint))
{
    builder.Services.AddSingleton<IGenerateImageService, AzureOpenAIGenerateImageService>();
}
else
{
    builder.Services.AddSingleton<IGenerateImageService, MockGenerateImageService>();
}

builder.Build().Run();
