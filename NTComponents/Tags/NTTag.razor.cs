using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Represents a bounded label tag for categories, statuses, and metadata.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="NTTag" /> for passive, non-interactive labels that identify a category, state, priority, or other compact piece of metadata.
///     </para>
///     <para>
///         Best practices: keep the label short and self-describing, choose semantic container/on-container color pairs with sufficient contrast, and do not rely on color alone to communicate meaning.
///         Use <see cref="NTChip" /> or another interactive control when the item needs selection, filtering, removal, navigation, or click behavior.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR preserves the component structure, styling, and accessibility semantics. Dynamic parameter changes require a new render.")]
public partial class NTTag {

    /// <summary>
    ///     Gets or sets the tag background color.
    /// </summary>
    /// <remarks>
    ///     Prefer semantic container colors, such as <see cref="NTColor.SecondaryContainer" />, <see cref="NTColor.SuccessContainer" />, or <see cref="NTColor.ErrorContainer" />, paired with the
    ///     matching on-container <see cref="TextColor" />.
    /// </remarks>
    [Parameter]
    public NTColor BackgroundColor { get; set; } = NTColor.SecondaryContainer;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-tag")
        .AddElevation(Elevation)
        .AddTextAlign(TextAlignment)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-tag-background-color", BackgroundColor)
        .AddVariable("nt-tag-text-color", TextColor)
        .Build();

    /// <summary>
    ///     Gets or sets the tag elevation level.
    /// </summary>
    /// <remarks>
    ///     The default elevation is <see cref="NTElevation.Lowest" />. Keep elevation low for inline metadata and use <see cref="NTElevation.None" /> when the tag sits inside an already elevated or strongly bounded surface.
    /// </remarks>
    [Parameter]
    public NTElevation Elevation { get; set; } = NTElevation.Lowest;

    /// <summary>
    ///     Gets or sets the tag label.
    /// </summary>
    /// <remarks>
    ///     The label is the tag's accessible text. Use concise visible text, such as <c>Draft</c>, <c>Blocked</c>, or <c>High priority</c>, rather than encoding meaning only through color.
    /// </remarks>
    [Parameter, EditorRequired]
    public string Label { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the text alignment within the tag.
    /// </summary>
    [Parameter]
    public NTTextAlign? TextAlignment { get; set; } = NTTextAlign.Center;

    /// <summary>
    ///     Gets or sets the tag text color.
    /// </summary>
    /// <remarks>
    ///     Pair this with <see cref="BackgroundColor" /> using the matching on-container color token whenever possible.
    /// </remarks>
    [Parameter]
    public NTColor TextColor { get; set; } = NTColor.OnSecondaryContainer;
}
