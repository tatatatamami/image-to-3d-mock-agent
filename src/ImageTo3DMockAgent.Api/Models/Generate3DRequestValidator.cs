using System.Collections.ObjectModel;

namespace ImageTo3DMockAgent.Api.Models;

public static class Generate3DRequestValidator
{
    private static readonly HashSet<string> AllowedOutputFormats = ["glb", "obj"];
    private static readonly HashSet<string> AllowedQualities = ["preview", "standard", "high"];

    public static IReadOnlyDictionary<string, string[]> Validate(Generate3DRequest? request)
    {
        Dictionary<string, string[]> errors = [];

        if (request is null)
        {
            errors["body"] = ["Request body is required."];
            return new ReadOnlyDictionary<string, string[]>(errors);
        }

        var hasImageUrl = !string.IsNullOrWhiteSpace(request.ImageUrl);
        var hasImageBlobPath = !string.IsNullOrWhiteSpace(request.ImageBlobPath);

        if (!hasImageUrl && !hasImageBlobPath)
        {
            errors["imageUrl"] = ["Either imageUrl or imageBlobPath is required."];
            errors["imageBlobPath"] = ["Either imageUrl or imageBlobPath is required."];
        }

        if (hasImageUrl && !IsValidImageUrl(request.ImageUrl!))
        {
            errors["imageUrl"] = ["imageUrl must be an absolute http or https URL."];
        }

        if (hasImageBlobPath && !IsValidBlobPath(request.ImageBlobPath!))
        {
            errors["imageBlobPath"] = ["imageBlobPath must be a relative blob path without path traversal segments."];
        }

        if (!string.IsNullOrWhiteSpace(request.OutputFormat) && !AllowedOutputFormats.Contains(request.GetOutputFormatOrDefault()))
        {
            errors["outputFormat"] = [$"outputFormat must be one of: {string.Join(", ", AllowedOutputFormats)}."];
        }

        if (!string.IsNullOrWhiteSpace(request.Quality) && !AllowedQualities.Contains(request.GetQualityOrDefault()))
        {
            errors["quality"] = [$"quality must be one of: {string.Join(", ", AllowedQualities)}."];
        }

        return new ReadOnlyDictionary<string, string[]>(errors);
    }

    private static bool IsValidImageUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

    private static bool IsValidBlobPath(string value)
    {
        var trimmed = value.Trim().Replace('\\', '/');
        if (trimmed.StartsWith('/'))
        {
            return false;
        }

        var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 &&
               segments.All(segment => segment is not "." and not "..") &&
               !trimmed.Contains('?') &&
               !trimmed.Contains('#');
    }
}
