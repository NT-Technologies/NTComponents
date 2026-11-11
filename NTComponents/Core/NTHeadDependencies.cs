using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Encodings.Web;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Meant to be placed in the head section of App.razor to include necessary dependencies for NTComponents.
/// </summary>
[ExcludeFromCodeCoverage]
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders head dependency links during static SSR.",
    CompatibilityDetails = "The component emits stylesheet, font, and script dependencies as head content without needing an interactive render mode.")]
public class NTHeadDependencies : IComponent {
    private const string DefaultTokenScope = ":root";
    private const string DarkThemeMedia = "(prefers-color-scheme: dark)";
    private const string LightThemeMedia = "(prefers-color-scheme: light)";
    private const string ActiveThemeElementId = "nt-theme-active-slot";
    private const string PendingThemeElementId = "nt-theme-pending-slot";
    private const string GoogleFontsStylesheetRoot = "https://fonts.googleapis.com/css2";
    // Only the axes the components use: FILL toggles, opsz follows font-size, weight and grade stay at their defaults. The full
    // axis ranges cost about 4 MB per icon family; this request is about 1.1 MB.
    private const string MaterialSymbolsAxes = "opsz,wght,FILL,GRAD@20..48,400,0..1,0";
    // Text swaps so it stays readable while Roboto loads; icons block so ligature names never flash in place of glyphs.
    private const string RobotoStylesheetHref = $"{GoogleFontsStylesheetRoot}?family=Roboto:wght@400;500;600;700&display=swap";
    private const NTFontFamily IconFontFamilies = NTFontFamily.MaterialSymbolsOutlined | NTFontFamily.MaterialSymbolsRounded | NTFontFamily.MaterialSymbolsSharp;
    private static readonly string?[] _iconFontStylesheetHrefs = new string?[((int)IconFontFamilies >> 1) + 1];
    private RenderHandle _renderHandle;
    private RenderFragment? _render;

    /// <summary>
    ///     Gets or sets the default contrast level when storage has no valid value.
    /// </summary>
    [Parameter]
    public NTThemeContrast DefaultContrast { get; set; } = NTThemeContrast.Default;

    /// <summary>
    ///     Gets or sets the default theme mode when storage has no valid value.
    /// </summary>
    [Parameter]
    public NTTheme DefaultTheme { get; set; } = NTTheme.System;

    /// <summary>
    ///     The CSS file name for the dark theme with default contrast.
    /// </summary>
    [Parameter]
    public string DarkDefaultCss { get; set; } = "dark.css";

    /// <summary>
    ///     The CSS file name for the dark theme with high contrast.
    /// </summary>
    [Parameter]
    public string DarkHighCss { get; set; } = "dark-hc.css";

    /// <summary>
    ///     The CSS file name for the dark theme with medium contrast.
    /// </summary>
    [Parameter]
    public string DarkMediumCss { get; set; } = "dark-mc.css";

    /// <summary>
    ///     Gets or sets the footer height.
    /// </summary>
    [Parameter]
    public double FooterHeight { get; set; } = 64;

    /// <summary>
    ///     Gets or sets a custom font stylesheet URL. When set, it replaces the generated Google Fonts stylesheets and their connection hints.
    /// </summary>
    [Parameter]
    public string? FontStylesheetHref { get; set; }

    /// <summary>
    ///     Gets or sets the default font families to request when <see cref="FontStylesheetHref" /> is not set.
    /// </summary>
    /// <remarks>
    ///     Roboto loads from its own stylesheet with <c>font-display: swap</c>. Material Symbols families share a second stylesheet
    ///     with <c>font-display: block</c>, a variable FILL (0..1) and optical size (20..48) axis, weight 400, and grade 0. Use
    ///     <see cref="FontStylesheetHref" /> to load other weight or grade values.
    /// </remarks>
    [Parameter]
    public NTFontFamily FontFamilies { get; set; } = NTFontFamily.All;

    /// <summary>
    ///     Gets or sets the header height.
    /// </summary>
    [Parameter]
    public double HeaderHeight { get; set; } = 64;

    /// <summary>
    ///     The CSS file name for the light theme with default contrast.
    /// </summary>
    [Parameter]
    public string LightDefaultCss { get; set; } = "light.css";

    /// <summary>
    ///     The CSS file name for the light theme with high contrast.
    /// </summary>
    [Parameter]
    public string LightHighCss { get; set; } = "light-hc.css";

    /// <summary>
    ///     The CSS file name for the light theme with medium contrast.
    /// </summary>
    [Parameter]
    public string LightMediumCss { get; set; } = "light-mc.css";

    /// <summary>
    ///     Gets or sets the side navigation width.
    /// </summary>
    [Parameter]
    public double SideNavWidth { get; set; } = 256;

    /// <summary>
    ///     Gets or sets the root path for theme CSS files.
    /// </summary>
    [Parameter]
    public string ThemesRoot { get; set; } = "/Themes";

    /// <summary>
    ///     Gets or sets the selector where NT measurement tokens are emitted.
    /// </summary>
    [Parameter]
    public string TokenScopeSelector { get; set; } = DefaultTokenScope;

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) => _renderHandle = renderHandle;

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        parameters.SetParameterProperties(this);
        _renderHandle.Render(_render ??= Render);
        return Task.CompletedTask;
    }

    internal static string NormalizeTokenScopeSelector(string? tokenScopeSelector) => string.IsNullOrWhiteSpace(tokenScopeSelector)
        ? DefaultTokenScope
        : tokenScopeSelector.Trim();

    internal static string CreateMeasurementTokens(double headerHeight, double footerHeight, double sideNavWidth, string? tokenScopeSelector) {
        return string.Create(CultureInfo.InvariantCulture, $"{NormalizeTokenScopeSelector(tokenScopeSelector)}{{--tnt-header-height:{headerHeight}px;--tnt-footer-height:{footerHeight}px;--tnt-side-nav-width:{sideNavWidth}px;}}");
    }

    internal static void RenderMeasurementTokens(RenderTreeBuilder builder, double headerHeight, double footerHeight, double sideNavWidth, string? tokenScopeSelector) {
        builder.OpenElement(0, "style");
        builder.AddAttribute(1, "id", "nt-measurement-tokens");
        builder.AddAttribute(2, "class", "tnt-measurements");
        builder.AddAttribute(3, "data-permanent", "nt-measurement-tokens");
        builder.AddContent(4, CreateMeasurementTokens(headerHeight, footerHeight, sideNavWidth, tokenScopeSelector));
        builder.CloseElement();
    }

    internal static string CreateThemeStylesheetHref(string? themesRoot, string? cssFile) {
        var root = string.IsNullOrWhiteSpace(themesRoot) ? "/Themes" : themesRoot.Trim();
        var file = string.IsNullOrWhiteSpace(cssFile) ? "light.css" : cssFile.Trim();
        return $"{root.TrimEnd('/')}/{file.TrimStart('/')}";
    }

    internal static string? CreateIconFontStylesheetHref(NTFontFamily fontFamilies) {
        // Only the icon family flags affect the URL, so each of the 7 possible URLs is built once per process.
        var index = (int)(fontFamilies & IconFontFamilies) >> 1;
        return index == 0 ? null : _iconFontStylesheetHrefs[index] ??= BuildIconFontStylesheetHref(fontFamilies);
    }

    private static string BuildIconFontStylesheetHref(NTFontFamily fontFamilies) {
        var families = new List<string>(3);

        if (fontFamilies.HasFlag(NTFontFamily.MaterialSymbolsOutlined)) {
            families.Add($"Material+Symbols+Outlined:{MaterialSymbolsAxes}");
        }
        if (fontFamilies.HasFlag(NTFontFamily.MaterialSymbolsRounded)) {
            families.Add($"Material+Symbols+Rounded:{MaterialSymbolsAxes}");
        }
        if (fontFamilies.HasFlag(NTFontFamily.MaterialSymbolsSharp)) {
            families.Add($"Material+Symbols+Sharp:{MaterialSymbolsAxes}");
        }

        return $"{GoogleFontsStylesheetRoot}?family={string.Join("&family=", families)}&display=block";
    }

    internal static string ResolveAssetPath(ResourceAssetCollection assets, string path) => assets[path];

    private void Render(RenderTreeBuilder builder) {
        var measurementStylesheetHref = ResolveAssetPath(_renderHandle.Assets, "_content/NTComponents/nt-measurements.css");

        // <style data-tnt-theme-critical="true">html, body, #app { background-color: Canvas; color: CanvasText; }</style>
        builder.OpenElement(0, "style");
        builder.AddAttribute(1, "data-tnt-theme-critical", "true");
        builder.AddAttribute(2, "data-nt-theme-critical", "true");
        builder.AddAttribute(3, "id", "nt-theme-critical");
        builder.AddAttribute(4, "data-permanent", "nt-theme-critical");
        builder.AddContent(5, "html, body, #app { background-color: Canvas; color: CanvasText; }");
        builder.CloseElement();

        builder.OpenRegion(7);
        RenderFirstPaintThemeLinks(builder);
        builder.CloseRegion();

        if (string.IsNullOrWhiteSpace(FontStylesheetHref) && (FontFamilies & NTFontFamily.All) != NTFontFamily.None) {
            builder.OpenRegion(9);
            RenderFontPreconnects(builder);
            builder.CloseRegion();
        }

        // Stable empty slots are preserved across enhanced navigation and activated by NTThemeRuntime as needed.
        builder.OpenRegion(20);
        RenderThemeSlot(builder, ActiveThemeElementId);
        builder.CloseRegion();
        builder.OpenRegion(23);
        RenderThemeSlot(builder, PendingThemeElementId);
        builder.CloseRegion();

        // <script type="application/json" id="nt-theme-config">...</script>
        builder.OpenElement(26, "script");
        builder.AddAttribute(27, "type", "application/json");
        builder.AddAttribute(28, "id", "nt-theme-config");
        builder.AddAttribute(29, "data-permanent", "nt-theme-config");
        builder.AddMarkupContent(30, CreateThemeConfiguration().ToJson());
        builder.CloseElement();

        // <script type="application/json" id="nt-theme-state">...</script>
        builder.OpenElement(31, "script");
        builder.AddAttribute(32, "type", "application/json");
        builder.AddAttribute(33, "id", "nt-theme-state");
        builder.AddAttribute(34, "data-permanent", "nt-theme-state");
        builder.AddMarkupContent(35, "{}");
        builder.CloseElement();

        // <link rel="preload" as="style" href="_content/NTComponents/nt-measurements.css">
        builder.OpenRegion(36);
        RenderMeasurementStylesheetPreload(builder, measurementStylesheetHref);
        builder.CloseRegion();

        // <script src="_content/NTComponents/NTTheme.js"></script>
        builder.OpenElement(37, "script");
        builder.AddAttribute(38, "id", "nt-theme-script");
        builder.AddAttribute(39, "src", ResolveAssetPath(_renderHandle.Assets, "_content/NTComponents/NTTheme.js"));
        builder.AddAttribute(40, "data-permanent", "nt-theme-script");
        builder.CloseElement();

        // <style class="tnt-measurements">...</style>
        builder.OpenRegion(41);
        RenderMeasurementTokens(builder, HeaderHeight, FooterHeight, SideNavWidth, TokenScopeSelector);
        builder.CloseRegion();

        // <link rel="stylesheet" href="_content/NTComponents/nt-measurements.css">
        builder.OpenElement(45, "link");
        builder.AddAttribute(46, "id", "nt-measurements-stylesheet");
        builder.AddAttribute(47, "rel", "stylesheet");
        builder.AddAttribute(48, "href", measurementStylesheetHref);
        builder.AddAttribute(49, "data-permanent", "nt-measurements-stylesheet");
        builder.CloseElement();

        // <link rel="stylesheet" href="_content/NTComponents/nt-ripple.css">
        builder.OpenElement(50, "link");
        builder.AddAttribute(51, "id", "nt-ripple-stylesheet");
        builder.AddAttribute(52, "rel", "stylesheet");
        builder.AddAttribute(53, "href", ResolveAssetPath(_renderHandle.Assets, "_content/NTComponents/nt-ripple.css"));
        builder.AddAttribute(54, "data-permanent", "nt-ripple-stylesheet");
        builder.CloseElement();

        builder.OpenRegion(55);
        RenderFontStylesheets(builder);
        builder.CloseRegion();

        // <script type="module">...</script>
        builder.OpenElement(56, "script");
        builder.AddAttribute(57, "id", "nt-anchor-positioning-loader");
        builder.AddAttribute(58, "type", "module");
        builder.AddAttribute(59, "data-permanent", "nt-anchor-positioning-loader");
        var anchorPositioningHref = JavaScriptEncoder.Default.Encode(ResolveAssetPath(_renderHandle.Assets, "_content/NTComponents/css-anchor-positioning.js"));
        builder.AddMarkupContent(60, $$"""
            if (!("anchorName" in document.documentElement.style)) {
                import("{{anchorPositioningHref}}");
            }
            """);
        builder.CloseElement();
    }

    private static void RenderMeasurementStylesheetPreload(RenderTreeBuilder builder, string href) {
        builder.OpenElement(0, "link");
        builder.AddAttribute(1, "id", "nt-measurements-preload");
        builder.AddAttribute(2, "rel", "preload");
        builder.AddAttribute(3, "as", "style");
        builder.AddAttribute(4, "href", href);
        builder.AddAttribute(5, "data-permanent", "nt-measurements-preload");
        builder.CloseElement();
    }

    private static void RenderFontPreconnects(RenderTreeBuilder builder) {
        builder.OpenElement(0, "link");
        builder.AddAttribute(1, "id", "nt-fonts-api-preconnect");
        builder.AddAttribute(2, "rel", "preconnect");
        builder.AddAttribute(3, "href", "https://fonts.googleapis.com");
        builder.AddAttribute(4, "data-permanent", "nt-fonts-api-preconnect");
        builder.CloseElement();

        builder.OpenElement(5, "link");
        builder.AddAttribute(6, "id", "nt-fonts-static-preconnect");
        builder.AddAttribute(7, "rel", "preconnect");
        builder.AddAttribute(8, "href", "https://fonts.gstatic.com");
        builder.AddAttribute(9, "crossorigin", string.Empty);
        builder.AddAttribute(10, "data-permanent", "nt-fonts-static-preconnect");
        builder.CloseElement();
    }

    private void RenderFontStylesheets(RenderTreeBuilder builder) {
        if (!string.IsNullOrWhiteSpace(FontStylesheetHref)) {
            builder.OpenRegion(0);
            RenderStylesheetLink(builder, "nt-fonts-stylesheet", ResolveAssetPath(_renderHandle.Assets, FontStylesheetHref.Trim()));
            builder.CloseRegion();
            return;
        }

        if (FontFamilies.HasFlag(NTFontFamily.Roboto)) {
            builder.OpenRegion(1);
            RenderStylesheetLink(builder, "nt-fonts-stylesheet", RobotoStylesheetHref);
            builder.CloseRegion();
        }

        var iconFontStylesheetHref = CreateIconFontStylesheetHref(FontFamilies);
        if (iconFontStylesheetHref is not null) {
            builder.OpenRegion(2);
            RenderStylesheetLink(builder, "nt-icon-fonts-stylesheet", iconFontStylesheetHref);
            builder.CloseRegion();
        }
    }

    private static void RenderStylesheetLink(RenderTreeBuilder builder, string id, string href) {
        builder.OpenElement(0, "link");
        builder.AddAttribute(1, "id", id);
        builder.AddAttribute(2, "rel", "stylesheet");
        builder.AddAttribute(3, "href", href);
        builder.AddAttribute(4, "data-permanent", id);
        builder.CloseElement();
    }

    private void RenderFirstPaintThemeLinks(RenderTreeBuilder builder) {
        if (DefaultTheme is NTTheme.Dark) {
            builder.OpenRegion(0);
            RenderFirstPaintThemeLink(builder, NTTheme.Dark, GetThemeCssFile(NTTheme.Dark), null);
            builder.CloseRegion();
            return;
        }

        if (DefaultTheme is NTTheme.Light) {
            builder.OpenRegion(0);
            RenderFirstPaintThemeLink(builder, NTTheme.Light, GetThemeCssFile(NTTheme.Light), null);
            builder.CloseRegion();
            return;
        }

        builder.OpenRegion(0);
        RenderFirstPaintThemeLink(builder, NTTheme.Light, GetThemeCssFile(NTTheme.Light), LightThemeMedia);
        builder.CloseRegion();
        builder.OpenRegion(1);
        RenderFirstPaintThemeLink(builder, NTTheme.Dark, GetThemeCssFile(NTTheme.Dark), DarkThemeMedia);
        builder.CloseRegion();
    }

    private void RenderFirstPaintThemeLink(RenderTreeBuilder builder, NTTheme theme, string cssFile, string? media) {
        builder.OpenElement(0, "link");
        builder.AddAttribute(1, "id", theme is NTTheme.Dark ? "nt-theme-default-dark" : "nt-theme-default-light");
        builder.AddAttribute(2, "rel", "stylesheet");
        builder.AddAttribute(3, "href", CreateThemeStylesheetHref(ThemesRoot, cssFile));
        builder.AddAttribute(4, "media", media);
        builder.AddAttribute(5, "data-nt-theme-default", "true");
        builder.AddAttribute(6, "data-permanent", theme is NTTheme.Dark ? "nt-theme-default-dark" : "nt-theme-default-light");
        builder.CloseElement();
    }

    private static void RenderThemeSlot(RenderTreeBuilder builder, string id) {
        builder.OpenElement(0, "link");
        builder.AddAttribute(1, "id", id);
        builder.AddAttribute(2, "data-permanent", id);
        builder.CloseElement();
    }

    private string GetThemeCssFile(NTTheme theme) => theme is NTTheme.Dark
        ? DefaultContrast switch {
            NTThemeContrast.Medium => DarkMediumCss,
            NTThemeContrast.High => DarkHighCss,
            _ => DarkDefaultCss
        }
        : DefaultContrast switch {
            NTThemeContrast.Medium => LightMediumCss,
            NTThemeContrast.High => LightHighCss,
            _ => LightDefaultCss
        };

    private NTThemeConfiguration CreateThemeConfiguration() => new() {
        DefaultContrast = DefaultContrast,
        DefaultTheme = DefaultTheme,
        ThemesRoot = ThemesRoot,
        LightDefaultCss = LightDefaultCss,
        LightMediumCss = LightMediumCss,
        LightHighCss = LightHighCss,
        DarkDefaultCss = DarkDefaultCss,
        DarkMediumCss = DarkMediumCss,
        DarkHighCss = DarkHighCss
    };
}
