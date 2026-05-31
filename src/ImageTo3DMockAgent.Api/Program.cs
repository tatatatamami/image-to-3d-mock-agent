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
builder.Services.AddSingleton<IGenerate3DAssetService, MockGenerate3DAssetService>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
