using ImageTo3DMockAgent.Api.Models;
using ImageTo3DMockAgent.Api.Options;
using ImageTo3DMockAgent.Api.Services;
using Microsoft.Extensions.Options;

namespace ImageTo3DMockAgent.Api.Tests;

public class Generate3DRequestValidatorTests
{
    [Fact]
    public void Validate_ReturnsErrors_WhenImageSourceIsMissing()
    {
        var errors = Generate3DRequestValidator.Validate(new Generate3DRequest());

        Assert.Contains("imageUrl", errors.Keys);
        Assert.Contains("imageBlobPath", errors.Keys);
    }

    [Fact]
    public void Validate_ReturnsErrors_ForUnsupportedFields()
    {
        var errors = Generate3DRequestValidator.Validate(new Generate3DRequest
        {
            ImageUrl = "ftp://example.com/sample.png",
            ImageBlobPath = "../sample.png",
            OutputFormat = "fbx",
            Quality = "ultra"
        });

        Assert.Contains("imageUrl", errors.Keys);
        Assert.Contains("imageBlobPath", errors.Keys);
        Assert.Contains("outputFormat", errors.Keys);
        Assert.Contains("quality", errors.Keys);
    }
}

public class MockGenerate3DAssetServiceTests
{
    private readonly MockGenerate3DAssetService service = new(Microsoft.Extensions.Options.Options.Create(new MockAssetStorageOptions
    {
        SourceImageBaseUrl = "https://storage.example.com/container",
        ModelBaseUrl = "https://storage.example.com/container"
    }));

    [Fact]
    public async Task GenerateAsync_UsesDefaultsAndImageUrl()
    {
        var response = await service.GenerateAsync(new Generate3DRequest
        {
            ImageUrl = "https://storage.example.com/container/images/sample.png"
        }, CancellationToken.None);

        Assert.Equal("https://storage.example.com/container/models/sample.glb", response.ModelUrl);
        Assert.Equal("models/sample.glb", response.ModelBlobPath);
        Assert.Equal("https://storage.example.com/container/images/sample.png", response.SourceImageUrl);
    }

    [Fact]
    public async Task GenerateAsync_DerivesSourceImageUrlFromBlobPath()
    {
        var response = await service.GenerateAsync(new Generate3DRequest
        {
            ImageBlobPath = "images/folder/sample.png",
            OutputFormat = "obj",
            Quality = "high"
        }, CancellationToken.None);

        Assert.Equal("https://storage.example.com/container/models/sample.obj", response.ModelUrl);
        Assert.Equal("models/sample.obj", response.ModelBlobPath);
        Assert.Equal("https://storage.example.com/container/images/folder/sample.png", response.SourceImageUrl);
    }
}