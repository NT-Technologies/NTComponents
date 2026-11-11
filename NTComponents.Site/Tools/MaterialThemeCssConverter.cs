using System.Text;
using System.Text.RegularExpressions;

namespace NTComponents.Site.Tools;

internal static partial class MaterialThemeCssConverter {
    private const string ExtendedColorPrefix = "--md-extended-color-";
    private const string SystemColorPrefix = "--md-sys-color-";
    private static readonly ThemeDefinition[] _themes = [
        new("light.css", ".light"),
        new("light-mc.css", ".light-medium-contrast"),
        new("light-hc.css", ".light-high-contrast"),
        new("dark.css", ".dark"),
        new("dark-mc.css", ".dark-medium-contrast"),
        new("dark-hc.css", ".dark-high-contrast")
    ];

    internal static IReadOnlyList<ThemeCssFile> ConvertFiles(IEnumerable<MaterialThemeSourceFile> sourceFiles) {
        var filesByName = sourceFiles
            .GroupBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var duplicateNames = filesByName.Where(file => file.Value.Length > 1).Select(file => file.Key).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        if (duplicateNames.Length > 0) {
            throw new MaterialThemeConversionException($"Remove duplicate files: {string.Join(", ", duplicateNames)}.");
        }

        var unsupportedNames = filesByName.Keys.Except(_themes.Select(theme => theme.FileName), StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        if (unsupportedNames.Length > 0) {
            throw new MaterialThemeConversionException($"Remove unsupported files: {string.Join(", ", unsupportedNames)}.");
        }

        var missingNames = _themes.Select(theme => theme.FileName).Except(filesByName.Keys, StringComparer.OrdinalIgnoreCase).ToArray();
        if (missingNames.Length > 0) {
            throw new MaterialThemeConversionException($"Add the missing Material Theme Builder files: {string.Join(", ", missingNames)}.");
        }

        return _themes.Select(theme => ConvertFile(filesByName[theme.FileName][0], theme)).ToArray();
    }

    private static ThemeCssFile ConvertFile(MaterialThemeSourceFile sourceFile, ThemeDefinition theme) {
        if (!SelectorRegex(theme.Selector).IsMatch(sourceFile.Content)) {
            throw new MaterialThemeConversionException($"{sourceFile.Name} must contain the {theme.Selector} color scheme.");
        }

        var properties = new List<KeyValuePair<string, string>>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in CustomPropertyRegex().Matches(sourceFile.Content)) {
            var convertedName = ConvertPropertyName(match.Groups["name"].Value);
            if (convertedName is null) {
                continue;
            }

            if (!names.Add(convertedName)) {
                throw new MaterialThemeConversionException($"{sourceFile.Name} produces duplicate {convertedName} properties.");
            }

            properties.Add(new(convertedName, match.Groups["value"].Value.Trim()));
        }

        if (!names.Contains("--nt-color-primary")) {
            throw new MaterialThemeConversionException($"{sourceFile.Name} does not contain Material 3 system color tokens.");
        }

        var output = new StringBuilder(4096);
        output.Append(":root {\r\n");
        foreach (var property in properties) {
            output.Append("    ").Append(property.Key).Append(": ").Append(property.Value).Append(";\r\n");
        }

        output.Append("}\r\n");
        return new() {
            Name = theme.FileName,
            Content = output.ToString(),
            TokenCount = properties.Count,
            Properties = properties.ToDictionary(property => property.Key, property => property.Value, StringComparer.Ordinal)
        };
    }

    private static string? ConvertPropertyName(string name) {
        if (name.StartsWith(SystemColorPrefix, StringComparison.Ordinal)) {
            return $"--nt-color-{name[SystemColorPrefix.Length..]}";
        }

        if (!name.StartsWith(ExtendedColorPrefix, StringComparison.Ordinal)) {
            return null;
        }

        var extendedName = name[ExtendedColorPrefix.Length..];
        return ConvertExtendedPropertyName(extendedName, "-on-color-container", "on-", "-container")
            ?? ConvertExtendedPropertyName(extendedName, "-color-container", string.Empty, "-container")
            ?? ConvertExtendedPropertyName(extendedName, "-on-color", "on-", string.Empty)
            ?? ConvertExtendedPropertyName(extendedName, "-color", string.Empty, string.Empty);
    }

    private static string? ConvertExtendedPropertyName(string name, string suffix, string prefix, string outputSuffix) => name.EndsWith(suffix, StringComparison.Ordinal) && name.Length > suffix.Length
        ? $"--nt-color-{prefix}{name[..^suffix.Length]}{outputSuffix}"
        : null;

    private static Regex SelectorRegex(string selector) => new($"(?:^|}})\\s*{Regex.Escape(selector)}\\s*{{", RegexOptions.CultureInvariant);

    [GeneratedRegex(@"(?<name>--[a-zA-Z0-9_-]+)\s*:\s*(?<value>[^;{}]+);", RegexOptions.CultureInvariant)]
    private static partial Regex CustomPropertyRegex();

    private sealed record ThemeDefinition(string FileName, string Selector);
}

internal sealed record MaterialThemeSourceFile(string Name, string Content);

internal sealed class ThemeCssFile {
    public string Content { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
    public int TokenCount { get; init; }
}

internal sealed class ThemeGenerationRequest {
    public string? Error { get; init; }
    public IReadOnlyDictionary<string, string> ExtendedColors { get; init; } = new Dictionary<string, string>();
    public bool HarmonizeExtendedColors { get; init; }
    public string? Neutral { get; init; }
    public string? NeutralVariant { get; init; }
    public string Primary { get; init; } = string.Empty;
    public string? PrimaryOverride { get; init; }
    public string? Secondary { get; init; }
    public string? Tertiary { get; init; }
    public string Variant { get; init; } = string.Empty;
}

internal sealed class MaterialThemeConversionException(string message) : Exception(message);
