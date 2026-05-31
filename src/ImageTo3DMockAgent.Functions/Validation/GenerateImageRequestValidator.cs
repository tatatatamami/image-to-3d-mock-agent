using ImageTo3DMockAgent.Functions.Models;
using System.Text.RegularExpressions;

namespace ImageTo3DMockAgent.Functions.Validation;

public sealed class GenerateImageRequestValidator
{
    private const string DefaultSize = "1024x1024";
    private const string DefaultQuality = "high";
    private static readonly Regex SizePattern = new("^[1-9]\\d{0,4}x[1-9]\\d{0,4}$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedQualities = new(StringComparer.OrdinalIgnoreCase)
    {
        "low",
        "medium",
        "high"
    };

    public GenerateImageRequest Normalize(GenerateImageRequest request)
    {
        return new GenerateImageRequest
        {
            Prompt = request.Prompt?.Trim(),
            Size = string.IsNullOrWhiteSpace(request.Size) ? DefaultSize : request.Size.Trim(),
            Quality = string.IsNullOrWhiteSpace(request.Quality) ? DefaultQuality : request.Quality.Trim().ToLowerInvariant(),
            N = request.N ?? 1
        };
    }

    public IReadOnlyDictionary<string, string[]> Validate(GenerateImageRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            AddError("prompt", "The prompt field is required.", errors);
        }

        if (string.IsNullOrWhiteSpace(request.Size) || !SizePattern.IsMatch(request.Size))
        {
            AddError("size", "The size field must be formatted as <width>x<height>.", errors);
        }

        if (string.IsNullOrWhiteSpace(request.Quality) || !AllowedQualities.Contains(request.Quality))
        {
            AddError("quality", "The quality field must be one of: low, medium, high.", errors);
        }

        if (request.N is null || request.N < 1)
        {
            AddError("n", "The n field must be greater than or equal to 1.", errors);
        }
        else if (request.N != 1)
        {
            AddError("n", "Only n=1 is supported by this endpoint.", errors);
        }

        return errors.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }

    private static void AddError(string fieldName, string message, IDictionary<string, List<string>> errors)
    {
        if (!errors.TryGetValue(fieldName, out var fieldErrors))
        {
            fieldErrors = [];
            errors[fieldName] = fieldErrors;
        }

        fieldErrors.Add(message);
    }
}
