using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Site.Tools;

namespace NTComponents.Site.Pages;

public partial class MaterialThemeConverter {
    private static readonly string[] _requiredFileNames = ["light.css", "light-mc.css", "light-hc.css", "dark.css", "dark-mc.css", "dark-hc.css"];
    private static readonly ThemeOption[] _themeStyles = [
        new("tonal-spot", "Tonal spot", "Balanced, low-to-medium colorfulness with a tertiary hue related to the source.", "General product interfaces and the safest default."),
        new("vibrant", "Vibrant", "Maximizes colorfulness in the primary palette and keeps supporting accents energetic.", "Consumer products, campaigns, and brands that need strong color."),
        new("expressive", "Expressive", "Intentionally shifts the palette away from the source hue to create stronger visual contrast.", "Editorial, playful, or personality-led experiences."),
        new("fidelity", "Fidelity", "Keeps the source color close to primary container and uses a complementary tertiary palette.", "Strict brand colors and products where the source color must stay recognizable."),
        new("content", "Content", "Keeps the source color close to primary container and chooses an analogous tertiary palette.", "Themes derived from imagery, artwork, products, or user content."),
        new("neutral", "Neutral", "Uses restrained chroma and keeps the whole system close to grayscale.", "Dense productivity tools and content-first interfaces."),
        new("monochrome", "Monochrome", "Removes chroma from the generated core palettes for a grayscale system.", "Minimal, photographic, or deliberately colorless experiences."),
        new("rainbow", "Rainbow", "Combines a colorful primary with separated accents and fully neutral surfaces.", "Playful experiences that need distinct accent families."),
        new("fruit-salad", "Fruit salad", "Shifts primary and secondary away from the source while letting tertiary echo it.", "Friendly, unconventional, and highly playful products.")
    ];
    private static readonly ThemeRoleGuide[] _roleGuides = [
        new("Primary", "primary, on-primary, primary-container", "Highest-emphasis branded actions, active states, selection, FABs, and important highlights.", "Prefer containers for larger areas. Pair every role with its matching on-color."),
        new("Secondary", "secondary, on-secondary, secondary-container", "Supporting actions, filters, tonal buttons, chips, and less prominent branded emphasis.", "Keep it subordinate to primary so the action hierarchy remains clear."),
        new("Tertiary", "tertiary, on-tertiary, tertiary-container", "A contrasting accent for highlights, badges, complementary data, or occasional feature emphasis.", "Use sparingly; it should add distinction without competing with primary."),
        new("Surfaces", "surface, surface-container-*, on-surface", "Page backgrounds, cards, sheets, dialogs, menus, and elevation expressed through container levels.", "Use surface-container levels instead of painting large layouts with primary."),
        new("Surface variant", "surface-variant, on-surface-variant, outline*", "Secondary text, quieter content, borders, dividers, and component boundaries.", "Use on-surface for primary text and on-surface-variant for supporting text."),
        new("Error", "error, on-error, error-container", "Validation failures, destructive actions, failed operations, and dangerous states.", "Do not use error for warnings or ordinary emphasis."),
        new("NTComponents status", "success, info, warning, assert", "Application-specific status messages and semantic accents not covered by Material core roles.", "Keep each meaning consistent throughout the product; harmonization may shift hue without changing meaning.")
    ];
    private static readonly ThemeSwatch[] _previewSwatches = [
        new("Primary", "--nt-color-primary", "--nt-color-on-primary"),
        new("Secondary", "--nt-color-secondary", "--nt-color-on-secondary"),
        new("Tertiary", "--nt-color-tertiary", "--nt-color-on-tertiary"),
        new("Surface", "--nt-color-surface-container", "--nt-color-on-surface"),
        new("Error", "--nt-color-error", "--nt-color-on-error")
    ];

    private readonly ThemeCreatorModel _creator = new();
    private readonly List<EditableThemeColor> _corePalettes = [
        new("primary", "Primary", "#6750a4"),
        new("secondary", "Secondary", "#625b71"),
        new("tertiary", "Tertiary", "#7d5260"),
        new("neutral", "Surface", "#605d62"),
        new("neutral-variant", "Surface variant", "#605d66"),
        new("error", "Error", "#ba1a1a")
    ];
    private readonly List<EditableThemeColor> _extendedColors = [
        new("success", "Success", "#00c853", true),
        new("info", "Information", "#0091ea", true),
        new("warning", "Warning", "#ffab00", true),
        new("assert", "Assert", "#aa00ff", true)
    ];
    private readonly List<MaterialThemeSourceFile> _sourceFiles = [];
    private IReadOnlyList<ThemeCssFile> _convertedFiles = [];
    private IReadOnlyList<ThemeCssFile> _generatedFiles = [];
    private IReadOnlyList<IBrowserFile>? _selectedFiles;
    private IJSObjectReference? _themeCreatorModule;
    private NTFileUpload? _upload;
    private string? _convertedDownloadUrl;
    private string? _converterErrorMessage;
    private string? _creatorErrorMessage;
    private string? _generatedDownloadUrl;
    private string _previewContrast = "default";
    private string _previewMode = "light";
    private string? _previewProjectName = "Northwind redesign";
    private int _generationVersion;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private IReadOnlyDictionary<string, object> ConvertedDownloadAttributes => DownloadAttributes(_convertedDownloadUrl, "ntcomponents-themes.zip");

    private IReadOnlyDictionary<string, object> GeneratedDownloadAttributes => DownloadAttributes(_generatedDownloadUrl, $"ntcomponents-theme-{_creator.Primary[1..]}.zip");

    private ThemeOption SelectedThemeStyle => _themeStyles.Single(option => option.Value == _creator.Variant);

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!firstRender) {
            return;
        }

        _themeCreatorModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/theme-creator.js");
        await _themeCreatorModule.InvokeVoidAsync("initializeThemePreview", "material-theme-creator", "material-theme-preview");
        await GenerateThemeAsync();
    }

    private async Task GenerateThemeAsync() {
        if (_themeCreatorModule is null) {
            return;
        }

        var generationVersion = ++_generationVersion;
        try {
            var request = CreateGenerationRequest();
            var files = await _themeCreatorModule.InvokeAsync<ThemeCssFile[]>("generateTheme", request);
            if (generationVersion != _generationVersion) {
                return;
            }

            _generatedFiles = files;
            _generatedDownloadUrl = $"data:application/zip;base64,{Convert.ToBase64String(await CreateArchiveAsync(files))}";
            _creatorErrorMessage = null;
            await _themeCreatorModule.InvokeVoidAsync("updateThemePreview", request, SelectedPreviewFileName);
            StateHasChanged();
        }
        catch (JSException exception) {
            if (generationVersion == _generationVersion) {
                _generatedFiles = [];
                _generatedDownloadUrl = null;
                _creatorErrorMessage = exception.Message;
                StateHasChanged();
            }
        }
    }

    private async Task ApplySelectedThemeAsync() {
        if (_themeCreatorModule is null) {
            return;
        }

        await _themeCreatorModule.InvokeVoidAsync("selectThemePreview", SelectedPreviewFileName);
    }

    private string SelectedPreviewFileName => $"{_previewMode}{(_previewContrast == "default" ? string.Empty : $"-{_previewContrast}")}.css";

    private ThemeGenerationRequest CreateGenerationRequest() => new() {
        Primary = _creator.Primary,
        PrimaryOverride = GetPaletteColor("primary"),
        Error = GetPaletteColor("error"),
        Secondary = GetPaletteColor("secondary"),
        Tertiary = GetPaletteColor("tertiary"),
        Neutral = GetPaletteColor("neutral"),
        NeutralVariant = GetPaletteColor("neutral-variant"),
        Variant = _creator.Variant,
        HarmonizeExtendedColors = _creator.HarmonizeExtendedColors,
        ExtendedColors = _extendedColors.ToDictionary(color => color.Key, color => color.Color, StringComparer.Ordinal)
    };

    private async Task UpdatePaletteAsync(EditableThemeColor palette, string? value) {
        palette.Color = value ?? palette.Color;
        palette.Enabled = true;
        await GenerateThemeAsync();
    }

    private async Task UpdatePrimaryAsync(string value) {
        _creator.Primary = value;
        await GenerateThemeAsync();
    }

    private async Task UpdateExtendedColorAsync(EditableThemeColor color, string? value) {
        color.Color = value ?? color.Color;
        await GenerateThemeAsync();
    }

    private async Task RestoreSiteThemeAsync(MouseEventArgs _) {
        if (_themeCreatorModule is not null) {
            await _themeCreatorModule.InvokeVoidAsync("resetSiteTheme");
        }
    }

    private async Task ResetCreatorAsync(MouseEventArgs _) {
        _creator.Reset();
        _corePalettes.ForEach(color => color.Reset());
        _extendedColors.ForEach(color => color.Reset());
        _previewMode = "light";
        _previewContrast = "default";
        await GenerateThemeAsync();
    }

    private string? GetPaletteColor(string key) => _corePalettes.Single(palette => palette.Key == key) is { Enabled: true } palette ? palette.Color : null;

    private async Task ReadSourceFileAsync(NTFileUploadEventArgs args) {
        if (args.Index == 0) {
            _sourceFiles.Clear();
            _convertedFiles = [];
            _convertedDownloadUrl = null;
            _converterErrorMessage = null;
        }

        if (args.Stream is null) {
            throw new InvalidOperationException($"Could not read {args.Name}.");
        }

        using var reader = new StreamReader(args.Stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        _sourceFiles.Add(new(args.Name, await reader.ReadToEndAsync()));
    }

    private Task OnFileErrorAsync(NTFileUploadEventArgs args) {
        _convertedFiles = [];
        _convertedDownloadUrl = null;
        _converterErrorMessage = args.ErrorMessage ?? $"Could not read {args.Name}.";
        return Task.CompletedTask;
    }

    private async Task OnConversionCompletedAsync(IReadOnlyList<NTFileUploadEventArgs> uploads) {
        if (uploads.Any(upload => !string.IsNullOrWhiteSpace(upload.ErrorMessage))) {
            return;
        }

        try {
            _convertedFiles = MaterialThemeCssConverter.ConvertFiles(_sourceFiles);
            _convertedDownloadUrl = $"data:application/zip;base64,{Convert.ToBase64String(await CreateArchiveAsync(_convertedFiles))}";
            _converterErrorMessage = null;
        }
        catch (MaterialThemeConversionException exception) {
            _convertedFiles = [];
            _convertedDownloadUrl = null;
            _converterErrorMessage = exception.Message;
        }
    }

    private async Task ClearConverterAsync(MouseEventArgs _) {
        _sourceFiles.Clear();
        _convertedFiles = [];
        _selectedFiles = null;
        _convertedDownloadUrl = null;
        _converterErrorMessage = null;
        if (_upload is not null) {
            await _upload.ClearAsync();
        }
    }

    private static IReadOnlyDictionary<string, object> DownloadAttributes(string? url, string fileName) => new Dictionary<string, object> {
        ["href"] = url ?? string.Empty,
        ["download"] = fileName,
        ["aria-disabled"] = string.IsNullOrWhiteSpace(url) ? "true" : "false"
    };

    private static async Task<byte[]> CreateArchiveAsync(IEnumerable<ThemeCssFile> files) {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true)) {
            foreach (var file in files) {
                var entry = archive.CreateEntry($"Themes/{file.Name}", CompressionLevel.Optimal);
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                await writer.WriteAsync(file.Content);
            }
        }

        return output.ToArray();
    }

    public async ValueTask DisposeAsync() {
        if (_themeCreatorModule is null) {
            return;
        }

        try {
            await _themeCreatorModule.InvokeVoidAsync("clearThemePreview");
            await _themeCreatorModule.DisposeAsync();
        }
        catch (JSDisconnectedException) {
        }
    }

    private sealed record ThemeOption(string Value, string Label, string Description, string BestFor);

    private sealed record ThemeRoleGuide(string Role, string Tokens, string UseFor, string Guidance);

    private sealed record ThemeSwatch(string Label, string Token, string OnToken);

    private sealed class ThemeCreatorModel {
        public bool HarmonizeExtendedColors { get; set; } = true;
        public string Primary { get; set; } = "#6750a4";
        public string Variant { get; set; } = "tonal-spot";

        public void Reset() {
            HarmonizeExtendedColors = true;
            Primary = "#6750a4";
            Variant = "tonal-spot";
        }
    }

    private sealed class EditableThemeColor(string key, string label, string color, bool enabled = false) {
        private readonly bool _defaultEnabled = enabled;
        private readonly string _defaultColor = color;

        public string Color { get; set; } = color;
        public bool Enabled { get; set; } = enabled;
        public string Key { get; } = key;
        public string Label { get; } = label;

        public void Reset() {
            Color = _defaultColor;
            Enabled = _defaultEnabled;
        }
    }
}
