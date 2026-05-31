using ImageTo3DMockAgent.Functions.Exceptions;
using ImageTo3DMockAgent.Functions.Models;
using ImageTo3DMockAgent.Functions.Services;
using ImageTo3DMockAgent.Functions.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace ImageTo3DMockAgent.Functions.Functions;

public sealed class GenerateImageFunction
{
    private readonly AzureOpenAIImageService _azureOpenAIImageService;
    private readonly BlobStorageService _blobStorageService;
    private readonly GenerateImageRequestValidator _validator;
    private readonly ILogger<GenerateImageFunction> _logger;

    public GenerateImageFunction(
        AzureOpenAIImageService azureOpenAIImageService,
        BlobStorageService blobStorageService,
        GenerateImageRequestValidator validator,
        ILogger<GenerateImageFunction> logger)
    {
        _azureOpenAIImageService = azureOpenAIImageService;
        _blobStorageService = blobStorageService;
        _validator = validator;
        _logger = logger;
    }

    [Function("generate_image")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "generate-image")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        GenerateImageRequest? payload;
        try
        {
            payload = await request.ReadFromJsonAsync<GenerateImageRequest>(cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Request body could not be parsed as JSON.");
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "invalid_json", "The request body must be valid JSON.", cancellationToken);
        }

        if (payload is null)
        {
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "invalid_request", "The request body is required.", cancellationToken);
        }

        var normalizedRequest = _validator.Normalize(payload);
        var validationErrors = _validator.Validate(normalizedRequest);
        if (validationErrors.Count > 0)
        {
            _logger.LogWarning(
                "Generate image request validation failed. FieldCount={FieldCount}, PromptLength={PromptLength}",
                validationErrors.Count,
                normalizedRequest.Prompt?.Length ?? 0);

            return await CreateErrorResponseAsync(
                request,
                HttpStatusCode.BadRequest,
                "validation_error",
                "One or more request fields are invalid.",
                cancellationToken,
                validationErrors);
        }

        try
        {
            var generatedImage = await _azureOpenAIImageService.GenerateImageAsync(normalizedRequest, cancellationToken);
            var uploadedImage = await _blobStorageService.UploadImageAsync(generatedImage.ImageBytes, generatedImage.CreatedAt, cancellationToken);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new GenerateImageResponse
            {
                ImageUrl = uploadedImage.BlobUrl,
                ImageBlobPath = uploadedImage.BlobPath,
                Prompt = normalizedRequest.Prompt!,
                Size = normalizedRequest.Size!,
                Quality = normalizedRequest.Quality!,
                Status = "succeeded",
                CreatedAt = generatedImage.CreatedAt
            }, cancellationToken);

            _logger.LogInformation(
                "Generate image request completed successfully. BlobPath={BlobPath}, PromptLength={PromptLength}",
                uploadedImage.BlobPath,
                normalizedRequest.Prompt!.Length);

            return response;
        }
        catch (ServiceOperationException ex)
        {
            _logger.LogError(ex, "Generate image request failed with error code {ErrorCode}", ex.ErrorCode);
            return await CreateErrorResponseAsync(request, ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generate image request failed unexpectedly.");
            return await CreateErrorResponseAsync(request, HttpStatusCode.InternalServerError, "internal_error", "An unexpected error occurred.", cancellationToken);
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string error,
        string message,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string[]>? details = null)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Error = error,
            Message = message,
            Details = details
        }, cancellationToken);

        return response;
    }
}
