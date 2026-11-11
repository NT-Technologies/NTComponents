using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents.Layout;

/// <summary>
///     Base class for modern Material 3 layout region components.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR preserves the component structure, styling, and accessibility semantics. Dynamic parameter changes require a new render.")]
public abstract class NTLayoutComponentBase : NTComponentBase {

    private readonly Dictionary<string, object> _elementAttributes = [];
    private string? _elementClass;
    private string? _elementStyle;
    private bool _elementStyleComputed;

    /// <summary>
    ///     Optional override for the layout region background color.
    /// </summary>
    [Parameter]
    public NTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     The child content to render inside the layout region.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Optional elevation applied to the layout region.
    /// </summary>
    [Parameter]
    public virtual NTElevation? Elevation { get; set; }

    /// <summary>
    ///     The HTML tag rendered for this layout region.
    /// </summary>
    [Parameter]
    public NTLayoutTag? Tag { get; set; }

    /// <summary>
    ///     Optional text alignment for the layout region.
    /// </summary>
    [Parameter]
    public NTTextAlign? TextAlignment { get; set; }

    /// <summary>
    ///     Optional override for the layout region text color.
    /// </summary>
    [Parameter]
    public NTColor? TextColor { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => _elementClass ?? BuildElementClass();

    /// <inheritdoc />
    public override string? ElementStyle {
        get {
            if (!_elementStyleComputed) {
                _elementStyle = BuildElementStyle();
                _elementStyleComputed = true;
            }

            return _elementStyle;
        }
    }

    /// <summary>
    ///     Component-specific CSS class.
    /// </summary>
    protected abstract string ComponentClass { get; }

    /// <summary>
    ///     Optional component state CSS class.
    /// </summary>
    protected virtual string? ComponentStateClass => null;

    /// <summary>
    ///     Component-specific CSS variable prefix.
    /// </summary>
    protected virtual string CssVariablePrefix => ComponentClass;

    /// <summary>
    ///     Default HTML tag rendered by the component.
    /// </summary>
    protected virtual NTLayoutTag DefaultTag => NTLayoutTag.Div;

    /// <summary>
    ///     Attributes rendered on the layout root element.
    /// </summary>
    protected IReadOnlyDictionary<string, object> ElementAttributes => _elementAttributes;

    /// <summary>
    ///     The resolved tag rendered by the Razor markup.
    /// </summary>
    protected NTLayoutTag ResolvedTag => Tag ?? DefaultTag;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _elementClass = BuildElementClass();
        _elementStyle = BuildElementStyle();
        _elementStyleComputed = true;
        BuildElementAttributes();
    }

    private string? BuildElementClass() => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass(ComponentClass)
        .AddClass(ComponentStateClass)
        .AddElevation(Elevation)
        .AddTextAlign(TextAlignment)
        .Build();

    private void BuildElementAttributes() {
        _elementAttributes.Clear();

        if (AdditionalAttributes is not null) {
            foreach (var (attributeName, attributeValue) in AdditionalAttributes) {
                _elementAttributes[attributeName] = attributeValue;
            }
        }

        SetAttribute(_elementAttributes, "class", _elementClass);
        SetAttribute(_elementAttributes, "style", _elementStyle);
        SetAttribute(_elementAttributes, "lang", ElementLang);
        SetAttribute(_elementAttributes, "id", ElementId);
        SetAttribute(_elementAttributes, "title", ElementTitle);
    }

    private string? BuildElementStyle() {
        if (!HasRenderedStyle) {
            return null;
        }

        return CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddVariable($"{CssVariablePrefix}-background-color", BackgroundColor.ToCssNTColorVariable(), BackgroundColor is not null)
            .AddVariable($"{CssVariablePrefix}-text-color", TextColor.ToCssNTColorVariable(), TextColor is not null)
            .Build();
    }

    private bool HasRenderedStyle =>
        AdditionalAttributes?.TryGetValue("style", out var style) == true && style is not null
        || BackgroundColor is not null
        || TextColor is not null;

    private static void SetAttribute(IDictionary<string, object> elementAttributes, string attributeName, object? attributeValue) {
        if (attributeValue is null) {
            elementAttributes.Remove(attributeName);
            return;
        }

        elementAttributes[attributeName] = attributeValue;
    }

}
