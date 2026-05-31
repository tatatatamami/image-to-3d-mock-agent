using System.Text.Json;
using ImageTo3DMockAgent.Api.Models;
using ImageTo3DMockAgent.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ImageTo3DMockAgent.Api.Functions;

public sealed class Generate3DFunction(IGenerate3DAssetService generate3DAssetService)
{
    [Function("Generate3D")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "generate-3d")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        Generate3DRequest? body;

        try
        {
            body = await request.ReadFromJsonAsync<Generate3DRequest>(cancellationToken);
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

        var errors = Generate3DRequestValidator.Validate(body);
        if (errors.Count > 0)
        {
            return new BadRequestObjectResult(new { errors });
        }

        var response = await generate3DAssetService.GenerateAsync(body!, cancellationToken);
        return new OkObjectResult(response);
    }
}
