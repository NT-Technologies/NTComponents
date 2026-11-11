using System.Text.Encodings.Web;

namespace NTComponents;

/// <summary>
///     Provides the preferred NTComponents theme runtime configuration.
/// </summary>
public sealed class NTThemeConfiguration {
    private static readonly JavaScriptEncoder _jsonEncoder = JavaScriptEncoder.Default;

    /// <summary>
    ///     Gets or sets the localStorage key used for persisted theme mode.
    /// </summary>
    public string ThemeStorageKey { get; set; } = "NTComponentsStoredThemeKey";

    /// <summary>
    ///     Gets or sets the localStorage key used for persisted contrast mode.
    /// </summary>
    public string ContrastStorageKey { get; set; } = "NTComponentsStoredContrastKey";

    /// <summary>
    ///     Gets or sets the default theme mode when storage has no valid value.
    /// </summary>
    public NTTheme DefaultTheme { get; set; } = NTTheme.System;

    /// <summary>
    ///     Gets or sets the default contrast level when storage has no valid value.
    /// </summary>
    public NTThemeContrast DefaultContrast { get; set; } = NTThemeContrast.Default;

    /// <summary>
    ///     Gets or sets the root path for theme CSS files.
    /// </summary>
    public string ThemesRoot { get; set; } = "/Themes";

    /// <summary>
    ///     Gets or sets the CSS file for the default light theme.
    /// </summary>
    public string LightDefaultCss { get; set; } = "light.css";

    /// <summary>
    ///     Gets or sets the CSS file for the medium contrast light theme.
    /// </summary>
    public string LightMediumCss { get; set; } = "light-mc.css";

    /// <summary>
    ///     Gets or sets the CSS file for the high contrast light theme.
    /// </summary>
    public string LightHighCss { get; set; } = "light-hc.css";

    /// <summary>
    ///     Gets or sets the CSS file for the default dark theme.
    /// </summary>
    public string DarkDefaultCss { get; set; } = "dark.css";

    /// <summary>
    ///     Gets or sets the CSS file for the medium contrast dark theme.
    /// </summary>
    public string DarkMediumCss { get; set; } = "dark-mc.css";

    /// <summary>
    ///     Gets or sets the CSS file for the high contrast dark theme.
    /// </summary>
    public string DarkHighCss { get; set; } = "dark-hc.css";

    internal string ToJson() => string.Concat([
        "{\"themeStorageKey\":\"", Encode(ThemeStorageKey),
        "\",\"contrastStorageKey\":\"", Encode(ContrastStorageKey),
        "\",\"defaultTheme\":\"", ToRuntimeValue(DefaultTheme),
        "\",\"defaultContrast\":\"", ToRuntimeValue(DefaultContrast),
        "\",\"themesRoot\":\"", Encode(ThemesRoot),
        "\",\"lightDefaultCss\":\"", Encode(LightDefaultCss),
        "\",\"lightMediumCss\":\"", Encode(LightMediumCss),
        "\",\"lightHighCss\":\"", Encode(LightHighCss),
        "\",\"darkDefaultCss\":\"", Encode(DarkDefaultCss),
        "\",\"darkMediumCss\":\"", Encode(DarkMediumCss),
        "\",\"darkHighCss\":\"", Encode(DarkHighCss),
        "\"}"
    ]);

    internal static string ToRuntimeValue(NTTheme theme) => theme switch {
        NTTheme.Light => "LIGHT",
        NTTheme.Dark => "DARK",
        _ => "SYSTEM"
    };

    internal static string ToRuntimeValue(NTThemeContrast contrast) => contrast switch {
        NTThemeContrast.Medium => "MEDIUM",
        NTThemeContrast.High => "HIGH",
        _ => "DEFAULT"
    };

    private static string Encode(string? value) => _jsonEncoder.Encode(value ?? string.Empty);
}
