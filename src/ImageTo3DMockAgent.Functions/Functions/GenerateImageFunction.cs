using System.Text.Json;
using ImageTo3DMockAgent.Functions.Models;
using ImageTo3DMockAgent.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ImageTo3DMockAgent.Functions.Functions;

public sealed class GenerateImageFunction(IGenerateImageService generateImageService)
{
    [Function("GenerateImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "generate-image")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        GenerateImageRequest? body;

        try
        {
            body = await request.ReadFromJsonAsync<GenerateImageRequest>(cancellationToken);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new
            {
                errors = new Dictionary<string, string[]>
                {
                    ["body"] = ["Request body must be valid JSON."]
                }
            });
        }

        var errors = GenerateImageRequestValidator.Validate(body);
        if (errors.Count > 0)
        {
            return new BadRequestObjectResult(new { errors });
        }

        try
        {
            var response = await generateImageService.GenerateAsync(body!, cancellationToken);
            return new OkObjectResult(response);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ObjectResult(new { error = "Image generation failed.", detail = ex.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
