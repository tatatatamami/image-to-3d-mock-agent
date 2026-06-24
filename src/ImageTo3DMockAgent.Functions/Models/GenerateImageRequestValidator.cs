using System.Collections.ObjectModel;

namespace ImageTo3DMockAgent.Functions.Models;

public static class GenerateImageRequestValidator
{
    private static readonly HashSet<string> AllowedSizes = ["1024x1024", "1024x1792", "1792x1024"];

    public static IReadOnlyDictionary<string, string[]> Validate(GenerateImageRequest? request)
    {
        Dictionary<string, string[]> errors = [];

        if (request is null)
        {
            errors["body"] = ["Request body is required."];
            return new ReadOnlyDictionary<string, string[]>(errors);
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            errors["prompt"] = ["prompt is required."];
        }

        if (!string.IsNullOrWhiteSpace(request.ReferenceImageUrl) && !IsValidUrl(request.ReferenceImageUrl))
        {
            errors["referenceImageUrl"] = ["referenceImageUrl must be an absolute http or https URL."];
        }

        if (!AllowedSizes.Contains(request.Size))
        {
            errors["size"] = [$"size must be one of: {string.Join(", ", AllowedSizes)}."];
        }

        return new ReadOnlyDictionary<string, string[]>(errors);
    }

    private static bool IsValidUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
