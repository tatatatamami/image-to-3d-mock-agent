using ImageTo3DMockAgent.Api.Models;

namespace ImageTo3DMockAgent.Api.Services;

public interface IGenerate3DAssetService
{
    Task<Generate3DResponse> GenerateAsync(Generate3DRequest request, CancellationToken cancellationToken);
}
