using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 tooltip component that displays passive contextual information when users hover over or focus on an element.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders tooltip content statically and enhances visibility and positioning with JavaScript.",
    CompatibilityDetails = "Static SSR emits the tooltip content. Hover and focus triggering, placement, dismissal, and show-hide timing depend on the isolated browser module.")]
public partial class NTTooltip : NTPageScriptComponent<NTTooltip> {

    private IReadOnlyDictionary<string, object>? _additionalAttributesWithoutInternalId;
    private string? _elementClass;
    private string? _elementStyle;
    private string _resolvedElementId = string.Empty;

    /// <summary>
    ///     Optional container color override. Material 3 defaults are used when unset.
    /// </summary>
    [Parameter]
    public NTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Optional border color override. Material 3 tooltips do not render a border when unset.
    /// </summary>
    [Parameter]
    public NTColor? BorderColor { get; set; }

    /// <summary>
    ///     The passive content to be displayed inside the tooltip.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     The delay in milliseconds before the tooltip appears when hovering over or focusing the parent element.
    /// </summary>
    [Parameter]
    public int ShowDelay { get; set; } = 500;

    /// <summary>
    ///     The delay in milliseconds before the tooltip disappears when pointer or focus leaves the parent element.
    /// </summary>
    [Parameter]
    public int HideDelay { get; set; } = 200;

    /// <inheritdoc />
    public override string? ElementClass => _elementClass;

    /// <inheritdoc />
    public override string? ElementStyle => _elementStyle;

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Tooltip/NTTooltip.razor.js";

    /// <summary>
    ///     Optional label color override. Material 3 defaults are used when unset.
    /// </summary>
    [Parameter]
    public NTColor? TextColor { get; set; }

    /// <summary>
    ///     The Material 3 tooltip variant to render.
    /// </summary>
    [Parameter]
    public NTTooltipVariant Variant { get; set; } = NTTooltipVariant.Plain;

    private string ResolvedElementId => _resolvedElementId;

    private IReadOnlyDictionary<string, object>? AdditionalAttributesWithoutInternalId => _additionalAttributesWithoutInternalId;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _additionalAttributesWithoutInternalId = GetAdditionalAttributesWithoutInternalId();
        _resolvedElementId = ElementId ?? $"{ComponentIdentifier}-tooltip";
        _elementClass = CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("nt-tooltip")
            .AddClass("nt-tooltip-rich", Variant == NTTooltipVariant.Rich)
            .Build();
        _elementStyle = CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddVariable("nt-tooltip-background-color", BackgroundColor.GetValueOrDefault(), BackgroundColor.HasValue)
            .AddVariable("nt-tooltip-text-color", TextColor.GetValueOrDefault(), TextColor.HasValue)
            .AddVariable("nt-tooltip-border-color", BorderColor.GetValueOrDefault(), BorderColor.HasValue)
            .AddVariable("nt-tooltip-border-width", "1px", BorderColor.HasValue)
            .AddVariable("nt-tooltip-show-delay", $"{ShowDelay}ms", ShowDelay != 500)
            .AddVariable("nt-tooltip-hide-delay", $"{HideDelay}ms", HideDelay != 200)
            .Build();
    }

    private IReadOnlyDictionary<string, object>? GetAdditionalAttributesWithoutInternalId() {
        if (AdditionalAttributes is null || !AdditionalAttributes.ContainsKey(NTComponentBase._ntCustomIdentifierAttribute)) {
            return AdditionalAttributes;
        }

        var attributes = new Dictionary<string, object>(AdditionalAttributes.Count - 1);
        foreach (var attribute in AdditionalAttributes) {
            if (attribute.Key != NTComponentBase._ntCustomIdentifierAttribute) {
                attributes[attribute.Key] = attribute.Value;
            }
        }

        return attributes;
    }
}

/// <summary>
///     Material 3 tooltip variants.
/// </summary>
public enum NTTooltipVariant {

    /// <summary>
    ///     A compact label that briefly identifies a control or action.
    /// </summary>
    Plain,

    /// <summary>
    ///     A larger informational container for passive supporting text.
    /// </summary>
    Rich
}
