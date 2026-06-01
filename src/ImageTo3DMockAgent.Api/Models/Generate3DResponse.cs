namespace ImageTo3DMockAgent.Api.Models;

public sealed record Generate3DResponse(
    string ModelUrl,
    string ModelBlobPath,
    string SourceImageUrl);
