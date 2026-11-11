using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Base component for rendering icons with configurable appearance, size, and color.
/// </summary>
/// <remarks>
///     NTIcon is the icon type accepted by NTComponents. Derived icons such as <see cref="MaterialIcon" /> and <see cref="NTSVGIcon" /> supply the icon content and styling.
/// </remarks>
public abstract class NTIcon : NTComponentBase {

    /// <summary>
    ///     Gets or sets the appearance style of the icon.
    /// </summary>
    /// <remarks>Controls the visual style of the icon such as Default, Outlined, Round, or Sharp.</remarks>
    [Parameter]
    public IconAppearance Appearance { get; set; } = IconAppearance.Default;

    /// <summary>
    ///     Gets or sets the color of the icon.
    /// </summary>
    /// <remarks>When <see langword="null" />, the icon inherits the surrounding text color.</remarks>
    [Parameter]
    public NTColor? Color { get; set; }

    /// <summary>
    ///     Gets or sets the icon content to be displayed.
    /// </summary>
    /// <remarks>This is a required parameter that specifies the actual icon content to render.</remarks>
    [Parameter, EditorRequired]
    public string Icon { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the size of the icon.
    /// </summary>
    /// <remarks>Defines the display size of the icon from predefined options. Defaults to Medium.</remarks>
    [Parameter]
    public IconSize Size { get; set; } = IconSize.Medium;

    /// <summary>
    ///     The content to display as a tooltip for the component.
    /// </summary>
    [Parameter]
    public RenderFragment? Tooltip { get; set; }

    private RenderFragment? _defaultFragment;

    /// <summary>
    ///     Additional CSS class to be applied to the icon element.
    /// </summary>
    internal string? AdditionalClass { get; set; }

    /// <summary>
    ///     Gets the title used when neither <c>ElementTitle</c> nor <see cref="Tooltip" /> is set.
    /// </summary>
    private protected virtual string? DefaultTitle => Icon;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTIcon" /> class with the specified icon.
    /// </summary>
    /// <param name="icon">The icon content to be displayed.</param>
    internal NTIcon(string icon) => Icon = icon;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTIcon" /> class.
    /// </summary>
    protected NTIcon() { }

    /// <summary>
    ///     Implicitly converts an icon to its string representation.
    /// </summary>
    /// <param name="icon">The icon to convert.</param>
    /// <returns>The string representation of the icon.</returns>
    public static implicit operator string(NTIcon icon) => icon.Icon;

    /// <summary>
    ///     Creates a render fragment for this icon.
    /// </summary>
    /// <returns>A <see cref="RenderFragment" /> that can be used to render this icon.</returns>
    public RenderFragment Render(string? additionalClass = null, bool suppressTitle = false) {
        if (additionalClass is null && !suppressTitle) {
            return _defaultFragment ??= builder => BuildRenderTree(builder, suppressTitle: false);
        }

        return new(builder => {
            if (additionalClass is not null) {
                AdditionalClass = additionalClass;
            }

            BuildRenderTree(builder, suppressTitle);
        });
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) => BuildRenderTree(builder, suppressTitle: false);

    /// <summary>
    ///     Adds the icon's content inside the icon element.
    /// </summary>
    private protected virtual void AddIconContent(RenderTreeBuilder builder, int sequence) => builder.AddContent(sequence, Icon);

    private void BuildRenderTree(RenderTreeBuilder builder, bool suppressTitle) {
        builder.OpenElement(0, "span");
        builder.AddMultipleAttributes(10, AdditionalAttributes);
        builder.AddAttribute(20, "class", ElementClass);
        builder.AddAttribute(30, "style", ElementStyle);
        builder.AddAttribute(40, "id", ElementId);
        builder.AddAttribute(50, "title", !suppressTitle && Tooltip is null ? ElementTitle ?? DefaultTitle : null);
        builder.AddElementReferenceCapture(60, e => Element = e);

        if (Tooltip is not null) {
            builder.OpenComponent<NTTooltip>(70);
            builder.AddComponentParameter(80, nameof(NTTooltip.ChildContent), Tooltip);
            builder.CloseComponent();
        }

        AddIconContent(builder, 90);

        builder.CloseElement();
    }
}
