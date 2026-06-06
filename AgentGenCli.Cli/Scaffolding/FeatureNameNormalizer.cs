using System.Globalization;
using System.Text.RegularExpressions;

namespace AgentGenCli.Cli.Scaffolding;

internal static partial class FeatureNameNormalizer
{
    public static FeatureNameInfo Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Feature name is required.", nameof(input));
        }

        var trimmed = input.Trim();
        if (!FeatureNamePattern().IsMatch(trimmed))
        {
            throw new ArgumentException(
                "Feature name must start with a letter and contain only letters, digits, hyphens, or underscores.",
                nameof(input)
            );
        }

        var segments = trimmed.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var pascal = string.Concat(segments.Select(ToPascalSegment));
        var folder = trimmed.ToLowerInvariant();

        return new FeatureNameInfo(trimmed, folder, pascal);
    }

    public static string ParseCrudLetters(string? crud, bool withDatabase)
    {
        if (!withDatabase)
        {
            if (!string.IsNullOrWhiteSpace(crud))
            {
                throw new ArgumentException("--crud requires --withDatabase.");
            }

            return string.Empty;
        }

        var letters = string.IsNullOrWhiteSpace(crud) ? "CRUD" : crud.ToUpperInvariant();
        var allowed = new HashSet<char> { 'C', 'R', 'U', 'D' };
        var result = new HashSet<char>();

        foreach (var letter in letters)
        {
            if (!allowed.Contains(letter))
            {
                throw new ArgumentException(
                    $"Invalid --crud letter '{letter}'. Allowed letters: C, R, U, D."
                );
            }

            result.Add(letter);
        }

        return new string(result.OrderBy(c => "CRUD".IndexOf(c)).ToArray());
    }

    private static string ToPascalSegment(string segment)
    {
        if (segment.Length == 0)
        {
            return string.Empty;
        }

        return char.ToUpper(segment[0], CultureInfo.InvariantCulture)
            + segment[1..].ToLowerInvariant();
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_-]*$")]
    private static partial Regex FeatureNamePattern();
}

internal sealed record FeatureNameInfo(string RawInput, string FolderName, string PascalName);
