using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 context menu wrapper that opens an <see cref="NTMenu" /> from pointer, keyboard, or long-press invocation.
/// </summary>
/// <remarks>
///     Use <see cref="TargetContent" /> for the interactive region (or <see cref="TargetElementId" /> to attach to an existing element) and <see cref="MenuContent" /> for standard <see cref="NTMenuButtonItem" />, <see cref="NTMenuAnchorItem" />,
///     <see cref="NTMenuLabelItem" />, <see cref="NTMenuDividerItem" />, and <see cref="NTMenuSubMenuItem" /> content.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires browser event handling for context-menu invocation.",
    CompatibilityDetails = "The target and menu markup can render statically, but pointer, keyboard, and long-press invocation depend on the isolated JavaScript module.")]
public partial class NTContextMenu {

    /// <summary>
    ///     Gets or sets the visual density of the opened menu.
    /// </summary>
    [Parameter]
    public NTMenuAppearance Appearance { get; set; } = NTMenuAppearance.Standard;

    /// <summary>
    ///     Gets or sets the accessible label for the context menu.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets whether item clicks should close the owning popover menu.
    /// </summary>
    [Parameter]
    public bool CloseOnContentClick { get; set; } = true;

    /// <summary>
    ///     Gets or sets an optional override for the menu container color.
    /// </summary>
    [Parameter]
    public NTColor? ContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets whether context menu invocation is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-context-menu")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Gets or sets the elevation level used by the floating menu surface.
    /// </summary>
    [Parameter]
    public NTElevation Elevation { get; set; } = NTElevation.Medium;

    /// <summary>
    ///     Gets or sets whether this context menu should append the menu content from ancestor <see cref="NTContextMenu" /> components.
    /// </summary>
    [Parameter]
    public bool InheritParentMenus { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Menus/NTContextMenu.razor.js";

    /// <summary>
    ///     Gets or sets the long-press delay in milliseconds for touch and pen context menu invocation.
    /// </summary>
    [Parameter]
    public int LongPressDelay { get; set; } = 500;

    /// <summary>
    ///     Gets or sets the context menu content.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment? MenuContent { get; set; }

    /// <summary>
    ///     Gets or sets the nearest ancestor context menu, when this context menu is nested inside another context menu target.
    /// </summary>
    [CascadingParameter]
    public NTContextMenu? ParentContextMenu { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected menu item container color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected menu item text and icon color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedTextColor { get; set; }

    /// <summary>
    ///     Gets or sets the id of an existing element elsewhere in the DOM that receives context menu gestures. When set,
    ///     <see cref="TargetContent" /> is not rendered. The element is looked up by id each time the component updates.
    /// </summary>
    [Parameter]
    public string? TargetElementId { get; set; }

    /// <summary>
    ///     Gets or sets an optional CSS selector that limits invocation to matching elements inside the target. Gestures elsewhere in the target leave the native context menu alone.
    /// </summary>
    [Parameter]
    public string? TargetSelector { get; set; }

    /// <summary>
    ///     Gets or sets the target content that receives context menu gestures. Provide either this or <see cref="TargetElementId" />.
    /// </summary>
    [Parameter]
    public RenderFragment? TargetContent { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the menu text and icon color.
    /// </summary>
    [Parameter]
    public NTColor? TextColor { get; set; }

    private IReadOnlyList<RenderFragment> InheritedParentMenuContent =>
        InheritParentMenus && ParentContextMenu is not null
            ? ParentContextMenu.EnumerateMenuContentWithAncestors().ToArray()
            : [];

    private string DisabledAttribute => Disabled ? "true" : "false";
    private string MenuId => $"{StableElementId}-menu";
    private string StableElementId => string.IsNullOrWhiteSpace(ElementId) ? ComponentIdentifier : ElementId;

    private IEnumerable<RenderFragment> EnumerateMenuContentWithAncestors() {
        if (MenuContent is not null) {
            yield return MenuContent;
        }

        if (ParentContextMenu is null) {
            yield break;
        }

        foreach (var menuContent in ParentContextMenu.EnumerateMenuContentWithAncestors()) {
            yield return menuContent;
        }
    }
}
