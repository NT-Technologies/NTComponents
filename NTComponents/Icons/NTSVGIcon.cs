using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Icon component that renders caller-supplied SVG markup.
/// </summary>
/// <remarks>
///     Set <see cref="NTIcon.Icon" /> to a complete <c>&lt;svg&gt;</c> element. The SVG is sized to <c>1em</c> so containers that size icons through <c>font-size</c> scale it, and it inherits
///     <c>currentColor</c> as its fill unless the SVG declares its own. The markup is rendered as-is without sanitization, so only pass trusted SVG content.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR emits the SVG markup inline, so the icon sizes and colors without JavaScript.")]
public sealed class NTSVGIcon : NTIcon {

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-icon")
        .AddClass("nt-svg-icon")
        .AddClass("mi-small", Size == IconSize.Small)
        .AddClass("mi-medium", Size == IconSize.Medium)
        .AddClass("mi-large", Size == IconSize.Large)
        .AddClass("mi-extra-large", Size == IconSize.ExtraLarge)
        .AddClass(AdditionalClass, !string.IsNullOrWhiteSpace(AdditionalClass))
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddStyle("color", Color.ToCssNTColorVariable(), Color.HasValue)
        .Build();

    /// <inheritdoc />
    private protected override string? DefaultTitle => null;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTSVGIcon" /> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NTSVGIcon() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTSVGIcon" /> class with the specified SVG markup.
    /// </summary>
    /// <param name="svg">The trusted SVG markup to render.</param>
    public NTSVGIcon(string svg) : base(svg) { }

    /// <inheritdoc />
    private protected override void AddIconContent(RenderTreeBuilder builder, int sequence) => builder.AddMarkupContent(sequence, Icon);
}
