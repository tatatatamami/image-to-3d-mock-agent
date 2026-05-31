using ImageTo3DMockAgent.Functions.Exceptions;
using ImageTo3DMockAgent.Functions.Models;
using ImageTo3DMockAgent.Functions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageTo3DMockAgent.Functions.Services;

public sealed class AzureOpenAIImageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAIImageService> _logger;

    public AzureOpenAIImageService(HttpClient httpClient, IConfiguration configuration, ILogger<AzureOpenAIImageService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ImageGenerationResult> GenerateImageAsync(GenerateImageRequest request, CancellationToken cancellationToken)
    {
        var options = GetOptions();
        var endpoint = BuildRequestUri(options);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                prompt = request.Prompt,
                size = request.Size,
                quality = request.Quality,
                n = request.N,
                response_format = "b64_json"
            }, SerializerOptions), Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Add("api-key", options.ApiKey);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _logger.LogInformation(
            "Generating image with Azure OpenAI deployment {Deployment}. Size={Size}, Quality={Quality}, Count={Count}, PromptLength={PromptLength}",
            options.ImageDeployment,
            request.Size,
            request.Quality,
            request.N,
            request.Prompt?.Length ?? 0);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Azure OpenAI request failed before a response was received.");
            throw new ServiceOperationException(HttpStatusCode.BadGateway, "azure_openai_request_failed", "Azure OpenAI image generation request failed.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Azure OpenAI image generation failed with status code {StatusCode}. Response={Response}",
                (int)response.StatusCode,
                errorBody);

            throw new ServiceOperationException(HttpStatusCode.BadGateway, "azure_openai_generation_failed", "Azure OpenAI image generation failed.");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        AzureOpenAIImageResponse? azureResponse;
        try
        {
            azureResponse = await JsonSerializer.DeserializeAsync<AzureOpenAIImageResponse>(responseStream, SerializerOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Azure OpenAI returned an unexpected response payload.");
            throw new ServiceOperationException(HttpStatusCode.BadGateway, "azure_openai_invalid_response", "Azure OpenAI returned an invalid response.", ex);
        }

        var base64Image = azureResponse?.Data?.FirstOrDefault()?.Base64Json;
        if (string.IsNullOrWhiteSpace(base64Image))
        {
            _logger.LogError("Azure OpenAI response did not contain b64_json image data.");
            throw new ServiceOperationException(HttpStatusCode.BadGateway, "azure_openai_missing_image_data", "Azure OpenAI response did not contain image data.");
        }

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(base64Image);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Azure OpenAI returned image data that was not valid base64.");
            throw new ServiceOperationException(HttpStatusCode.BadGateway, "azure_openai_invalid_image_data", "Azure OpenAI returned invalid image data.", ex);
        }

        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            CreatedAt = azureResponse?.Created is > 0
                ? DateTimeOffset.FromUnixTimeSeconds(azureResponse.Created.Value)
                : DateTimeOffset.UtcNow
        };
    }

    private AzureOpenAIOptions GetOptions()
    {
        var options = new AzureOpenAIOptions
        {
            Endpoint = _configuration["AZURE_OPENAI_ENDPOINT"] ?? string.Empty,
            ApiKey = _configuration["AZURE_OPENAI_API_KEY"] ?? string.Empty,
            ImageDeployment = _configuration["AZURE_OPENAI_IMAGE_DEPLOYMENT"] ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(options.Endpoint) || string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.ImageDeployment))
        {
            throw new ServiceOperationException(HttpStatusCode.InternalServerError, "configuration_error", "Azure OpenAI configuration is missing or incomplete.");
        }

        return options;
    }

    private static Uri BuildRequestUri(AzureOpenAIOptions options)
    {
        var baseUri = new Uri(options.Endpoint.TrimEnd('/') + "/", UriKind.Absolute);
        var relativePath = $"openai/deployments/{Uri.EscapeDataString(options.ImageDeployment)}/images/generations?api-version={AzureOpenAIOptions.ApiVersion}";
        return new Uri(baseUri, relativePath);
    }

    private sealed class AzureOpenAIImageResponse
    {
        [JsonPropertyName("created")]
        public long? Created { get; init; }

        [JsonPropertyName("data")]
        public List<AzureOpenAIImageData>? Data { get; init; }
    }

    private sealed class AzureOpenAIImageData
    {
        [JsonPropertyName("b64_json")]
        public string? Base64Json { get; init; }
    }
}
