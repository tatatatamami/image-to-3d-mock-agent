using ImageTo3DMockAgent.Functions.Models;

namespace ImageTo3DMockAgent.Functions.Services;

public interface IGenerateImageService
{
    Task<GenerateImageResponse> GenerateAsync(GenerateImageRequest request, CancellationToken cancellationToken);
}
