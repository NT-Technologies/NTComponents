using System.Diagnostics.CodeAnalysis;
using NTComponents.Interfaces;

namespace NTComponents.Core;

/// <summary>
///     A builder class for constructing CSS class strings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CssClassBuilder {
    private static readonly string[] _legacyElevationClasses = ["tnt-elevation-0", "tnt-elevation-1", "tnt-elevation-2", "tnt-elevation-3", "tnt-elevation-4", "tnt-elevation-5", "tnt-elevation-6", "tnt-elevation-7", "tnt-elevation-8", "tnt-elevation-9", "tnt-elevation-10"];

    // Class lists are short, so a list with a linear duplicate check is cheaper than a hash set and keeps insertion order.
    private readonly List<string> _classes = [];

    /// <summary>
    ///     Creates a new instance of <see cref="CssClassBuilder" /> with an optional default class.
    /// </summary>
    /// <param name="defaultClass">The default CSS class to add.</param>
    /// <returns>A new instance of <see cref="CssClassBuilder" />.</returns>
    public static CssClassBuilder Create(string defaultClass = "tnt-components") => new CssClassBuilder().AddClass(defaultClass);

    /// <summary>
    ///     Adds a CSS class if the specified condition is met.
    /// </summary>
    /// <param name="className">The CSS class to add.</param>
    /// <param name="when">     The condition to check.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddClass(string? className, bool? when = true) {
        if (when == true && !string.IsNullOrWhiteSpace(className) && !_classes.Contains(className)) {
            _classes.Add(className);
        }
        return this;
    }

    /// <summary>
    ///     Adds a CSS class for NT corner radius.
    /// </summary>
    /// <param name="cornerRadius">The NT corner radius token.</param>
    /// <param name="enabled">     Whether the class should be added.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddCornerRadius(NTCornerRadius cornerRadius, bool enabled = true) => AddClass(cornerRadius.ToCssClass(), enabled);

    /// <summary>
    ///     Adds a CSS class for disabled state.
    /// </summary>
    /// <param name="disabled">Whether the component is disabled.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddDisabled(bool disabled) => AddClass("tnt-disabled", disabled);

    /// <summary>
    ///     Adds a CSS class for elevation.
    /// </summary>
    /// <param name="elevation">The elevation level.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddElevation(int elevation) => elevation < 0 ? this : AddClass(_legacyElevationClasses[Math.Min(elevation, 10)]);

    /// <summary>
    ///     Adds a CSS class for NT elevation.
    /// </summary>
    /// <param name="elevation">The NT elevation level.</param>
    /// <param name="enabled">  Whether the class should be added.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddElevation(NTElevation elevation, bool enabled = true) => AddClass(elevation.ToCssClass(), enabled);

    /// <summary>
    ///     Adds an elevation CSS class to the builder.
    /// </summary>
    /// <param name="elevation">The elevation level to apply.</param>
    /// <param name="enabled">  Determines whether the elevation class is added. Default is <see langword="true" />.</param>
    /// <returns>The current <see cref="CssClassBuilder" /> instance for method chaining.</returns>
    public CssClassBuilder AddElevation(NTElevation? elevation, bool enabled = true) => AddClass(elevation.GetValueOrDefault().ToCssClass(), enabled && elevation.HasValue);

    /// <summary>
    ///     Adds a CSS class for filled state.
    /// </summary>
    /// <param name="enabled">Whether the filled state is enabled.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddFilled(bool enabled = true) => AddClass("tnt-filled", enabled);

    /// <summary>
    ///     Adds CSS classes from additional attributes.
    /// </summary>
    /// <param name="additionalAttributes">The additional attributes.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddFromAdditionalAttributes(IReadOnlyDictionary<string, object>? additionalAttributes) => additionalAttributes?.TryGetValue("class", out var @class) == true && @class is not null ? AddClass(@class.ToString()) : this;

    /// <summary>
    ///     Adds a CSS class for ripple effect.
    /// </summary>
    /// <param name="enabled">Whether the ripple effect is enabled.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddRipple(bool enabled = true) => AddClass("tnt-ripple", enabled);

    /// <summary>
    ///     Adds a CSS class for size.
    /// </summary>
    /// <param name="size">The size to use.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddSize(NTSize? size) {
        return size switch {
            null => this,
            NTSize.Smallest => AddClass("tnt-size-xs"),
            NTSize.Small => AddClass("tnt-size-s"),
            NTSize.Medium => AddClass("tnt-size-m"),
            NTSize.Large => AddClass("tnt-size-l"),
            NTSize.Largest => AddClass("tnt-size-xl"),
            _ => AddClass("tnt-size-")
        };
    }

    /// <summary>
    ///     Adds a CSS class for text alignment.
    /// </summary>
    /// <param name="textAlign">The text alignment to use.</param>
    /// <returns>The current instance of <see cref="CssClassBuilder" />.</returns>
    public CssClassBuilder AddTextAlign(NTTextAlign? textAlign) => textAlign is null ? this : AddClass("tnt-text-align-" + textAlign.Value.ToString().ToLowerInvariant());

    /// <summary>
    ///     Builds the CSS class string.
    /// </summary>
    /// <returns>The constructed CSS class string.</returns>
    public string Build() => string.Join(' ', _classes).Trim();
}
