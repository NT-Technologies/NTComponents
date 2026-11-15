using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 Expressive FAB menu that opens from a floating action button to display 2-6 related actions.
/// </summary>
/// <remarks>
///     <para>
///         FAB menus should be used for a small set of closely related, prominent actions. They open from a FAB and should not be used with extended FABs or unrelated overflow actions.
///     </para>
///     <para>
///         The trigger uses native popover attributes so it can open and close in static SSR. The browser owns the open state. The isolated script enhances focus, ripple registration, item-click
///         closing, and reports open-state changes through <see cref="OnExpandedChanged" /> when an interactive runtime is available.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a native popover-backed FAB menu and enhances state sync with JavaScript.",
    CompatibilityDetails = "Static SSR emits the FAB trigger and popover panel. The browser module adds focus handling, item-click closing, ripple registration, and open-state change notifications to Blazor.")]
public partial class NTFabMenu {

    /// <summary>
    ///     Gets or sets the accessible name that describes the menu opened by the FAB.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the closed FAB container color.
    /// </summary>
    /// <remarks>
    ///     Leave unset to use the primary container default. When overriding, pair this with a matching <see cref="TextColor" /> from the same Material color role family, such as
    ///     <see cref="NTColor.SecondaryContainer" /> with <see cref="NTColor.OnSecondaryContainer" />. The closed FAB, selected FAB, and menu items should use one coordinated primary, secondary,
    ///     or tertiary set rather than mixing unrelated roles.
    /// </remarks>
    [Parameter]
    public NTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the source FAB size.
    /// </summary>
    [Parameter]
    public NTSize ButtonSize { get; set; } = NTSize.Small;

    /// <summary>
    ///     Gets or sets the menu content rendered when the FAB is expanded.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets whether selecting an enabled item closes the menu.
    /// </summary>
    [Parameter]
    public bool CloseOnMenuContentClick { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether the FAB menu is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-fab-menu")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass(Placement switch {
            NTFabButtonPlacement.Inline => "nt-fab-menu-placement-inline",
            NTFabButtonPlacement.LowerRight => "nt-fab-menu-placement-lower-right",
            NTFabButtonPlacement.LowerLeft => "nt-fab-menu-placement-lower-left",
            NTFabButtonPlacement.UpperRight => "nt-fab-menu-placement-upper-right",
            NTFabButtonPlacement.UpperLeft => "nt-fab-menu-placement-upper-left",
            _ => throw new ArgumentOutOfRangeException(nameof(Placement), Placement, null)
        })
        .AddSize(EffectiveButtonSize)
        .AddDisabled(Disabled)
        .Build();

    /// <summary>
    ///     Gets or sets the callback invoked after the menu opens (<see langword="true" />) or closes (<see langword="false" />).
    /// </summary>
    /// <remarks>
    ///     The open state lives in the browser popover. This reports every change, whether it comes from the user or from <see cref="OpenAsync" /> and <see cref="CloseAsync" />.
    /// </remarks>
    [Parameter]
    public EventCallback<bool> OnExpandedChanged { get; set; }

    /// <summary>
    ///     Gets or sets the optional native name attribute for the FAB toggle button.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-fab-menu-fab-bg", BackgroundColor.ToCssNTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-fab-menu-fab-fg", TextColor.ToCssNTColorVariable(), TextColor.HasValue)
        .AddVariable("nt-fab-menu-selected-fab-bg", SelectedFabBackgroundColor.ToCssNTColorVariable(), SelectedFabBackgroundColor.HasValue)
        .AddVariable("nt-fab-menu-selected-fab-fg", SelectedFabTextColor.ToCssNTColorVariable(), SelectedFabTextColor.HasValue)
        .AddVariable("nt-fab-menu-item-bg", MenuItemBackgroundColor.ToCssNTColorVariable(), MenuItemBackgroundColor.HasValue)
        .AddVariable("nt-fab-menu-item-fg", MenuItemTextColor.ToCssNTColorVariable(), MenuItemTextColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets the resting elevation for the FAB and close button.
    /// </summary>
    [Parameter]
    public NTElevation Elevation { get; set; } = NTElevation.Medium;

    /// <summary>
    ///     Gets or sets whether ripple effects should render.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Gets or sets the icon rendered in the closed FAB.
    /// </summary>
    [Parameter, EditorRequired]
    public NTIcon Icon { get; set; } = default!;

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Buttons/NTFabMenu.razor.js";

    /// <summary>
    ///     Gets or sets an optional override for menu item container color.
    /// </summary>
    /// <remarks>
    ///     Leave unset to match the closed FAB container default. Material FAB menus work best when menu items use the container tone of the chosen color set, such as
    ///     <see cref="NTColor.PrimaryContainer" />, <see cref="NTColor.SecondaryContainer" />, or <see cref="NTColor.TertiaryContainer" />. Pair this with the corresponding
    ///     <see cref="MenuItemTextColor" /> value.
    /// </remarks>
    [Parameter]
    public NTColor? MenuItemBackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for menu item content color.
    /// </summary>
    /// <remarks>
    ///     Leave unset to match the closed FAB content default. Use the matching on-container role for the menu item container, such as <see cref="NTColor.OnPrimaryContainer" />,
    ///     <see cref="NTColor.OnSecondaryContainer" />, or <see cref="NTColor.OnTertiaryContainer" />. Avoid low-contrast custom pairings.
    /// </remarks>
    [Parameter]
    public NTColor? MenuItemTextColor { get; set; }

    /// <summary>
    ///     Gets or sets where the FAB menu is positioned.
    /// </summary>
    [Parameter]
    public NTFabButtonPlacement Placement { get; set; } = NTFabButtonPlacement.LowerRight;

    /// <summary>
    ///     Gets or sets an optional override for the selected FAB container color.
    /// </summary>
    /// <remarks>
    ///     Leave unset to use the primary selected default. The selected FAB is the expanded close button and should use the stronger base role of the chosen set, such as
    ///     <see cref="NTColor.Primary" />, <see cref="NTColor.Secondary" />, or <see cref="NTColor.Tertiary" />. Pair this with the matching <see cref="SelectedFabTextColor" /> value so the close
    ///     icon remains legible.
    /// </remarks>
    [Parameter]
    public NTColor? SelectedFabBackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected FAB content color.
    /// </summary>
    /// <remarks>
    ///     Leave unset to use the primary selected content default. Use the matching on-color role for the selected FAB container, such as <see cref="NTColor.OnPrimary" />,
    ///     <see cref="NTColor.OnSecondary" />, or <see cref="NTColor.OnTertiary" />.
    /// </remarks>
    [Parameter]
    public NTColor? SelectedFabTextColor { get; set; }

    /// <summary>
    ///     Gets or sets whether click events should stop propagating on the FAB toggle.
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the closed FAB content color.
    /// </summary>
    /// <remarks>
    ///     Leave unset to use the primary container content default. Use the matching on-container role for <see cref="BackgroundColor" />, such as <see cref="NTColor.OnPrimaryContainer" />,
    ///     <see cref="NTColor.OnSecondaryContainer" />, or <see cref="NTColor.OnTertiaryContainer" />.
    /// </remarks>
    [Parameter]
    public NTColor? TextColor { get; set; }

    internal NTSize EffectiveButtonSize => ButtonSize switch {
        NTSize.Smallest => NTSize.Small,
        NTSize.Small => NTSize.Small,
        NTSize.Medium => NTSize.Medium,
        NTSize.Large => NTSize.Large,
        NTSize.Largest => NTSize.Large,
        _ => NTSize.Small
    };

    private string CloseOnMenuContentClickAttribute => CloseOnMenuContentClick ? "true" : "false";
    private string EffectiveAriaLabel => AriaLabel?.Trim() ?? string.Empty;
    private string MenuAnchorName => $"--{StableElementId}-anchor";
    private string MenuButtonId => $"{StableElementId}-button";
    internal string MenuId => $"{StableElementId}-menu";
    private string MenuPanelStyle => $"position-anchor: {MenuAnchorName};";
    private string? MenuPopoverTarget => Disabled ? null : MenuId;
    private string StableElementId => string.IsNullOrWhiteSpace(ElementId) ? ComponentIdentifier : ElementId;
    private string ToggleButtonClass => "nt-fab-menu-button " + Elevation.ToCssClass();
    private string ToggleButtonStyle => $"anchor-name: {MenuAnchorName};";
    private NTIcon CloseIcon => _closeIcon ??= MaterialIcon.Close;

    private static readonly RenderFragment ButtonRipple = NTRipple.RenderHost();
    private readonly List<IFabMenuItem> _menuItems = [];
    private NTIcon? _closeIcon;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (Icon is null) {
            throw new ArgumentNullException(nameof(Icon), "NTFabMenu requires a non-null Icon parameter.");
        }

        if (string.IsNullOrWhiteSpace(AriaLabel)) {
            throw new ArgumentException("NTFabMenu requires a non-empty AriaLabel that describes the menu opened by the FAB.", nameof(AriaLabel));
        }

        if (BackgroundColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(BackgroundColor)} must be a visible color.");
        }

        if (TextColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(TextColor)} must be a visible color.");
        }

        if (SelectedFabBackgroundColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(SelectedFabBackgroundColor)} must be a visible color.");
        }

        if (SelectedFabTextColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(SelectedFabTextColor)} must be a visible color.");
        }

        if (MenuItemBackgroundColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(MenuItemBackgroundColor)} must be a visible color.");
        }

        if (MenuItemTextColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(MenuItemTextColor)} must be a visible color.");
        }

        if (ButtonSize is NTSize.Smallest) {
            Debug.WriteLine($"{nameof(NTFabMenu)} does not support {nameof(NTSize.Smallest)}. Rendering with {nameof(NTSize.Small)}.");
        } else if (ButtonSize is NTSize.Largest) {
            Debug.WriteLine($"{nameof(NTFabMenu)} does not support {nameof(NTSize.Largest)}. Rendering with {nameof(NTSize.Large)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            var count = _menuItems.Count;
            if (count < 2 || count > 6) {
                throw new InvalidOperationException($"{nameof(NTFabMenu)} requires 2 to 6 {nameof(NTFabMenuButtonItem)} or {nameof(NTFabMenuAnchorItem)} children.");
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    ///     Refreshes the rendered FAB menu items.
    /// </summary>
    /// <remarks>
    ///     Use this when child item metadata changes outside the normal parent render flow and the menu should immediately
    ///     re-render the registered item list.
    /// </remarks>
    public Task RefreshAsync() => InvokeAsync(StateHasChanged);

    /// <summary>
    ///     Opens the menu. Does nothing while the menu is disabled or before it is interactive.
    /// </summary>
    public Task OpenAsync() => Disabled ? Task.CompletedTask : SetOpenAsync(true);

    /// <summary>
    ///     Closes the menu. Does nothing before the menu is interactive.
    /// </summary>
    public Task CloseAsync() => SetOpenAsync(false);

    /// <summary>
    ///     Receives native popover state changes from the browser.
    /// </summary>
    [JSInvokable]
    public async Task NotifyFabMenuExpandedChanged(bool expanded) {
        await OnExpandedChanged.InvokeAsync(expanded);
    }

    private async Task SetOpenAsync(bool open) {
        if (TryGetInteropReferences(out var module, out _)) {
            try {
                await module.InvokeVoidAsync("setOpen", Element, open);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore.
            }
        }
    }

    /// <summary>
    ///     Tracks a registered menu item so the FAB menu can render a constrained menu layout.
    /// </summary>
    internal void RegisterMenuItem(IFabMenuItem item) {
        if (item is not null && !_menuItems.Contains(item)) {
            _menuItems.Add(item);
            StateHasChanged();
        }
    }

    /// <summary>
    ///     Unregisters a menu item when it is removed.
    /// </summary>
    internal void UnregisterMenuItem(IFabMenuItem item) {
        if (item is not null && _menuItems.Remove(item)) {
            StateHasChanged();
        }
    }

    internal string GetMenuItemAriaLabel(IFabMenuItem item) => string.IsNullOrWhiteSpace(item.AriaLabel) ? item.Label : item.AriaLabel;

    internal string GetMenuItemClass(IFabMenuItem item) => CssClassBuilder.Create("nt-fab-menu-item")
        .AddClass(TryGetAdditionalAttribute(item, "class", out var @class) ? @class?.ToString() : null)
        .AddClass("nt-fab-menu-item-disabled", IsMenuItemDisabled(item))
        .AddClass("nt-fab-menu-item-without-icon", item.Icon is null)
        .Build();

    internal bool IsMenuItemDisabled(IFabMenuItem item) => Disabled || item.Disabled;

    internal void RenderMenuItemContent(RenderTreeBuilder builder, IFabMenuItem item) {
        if (item.Icon is not null) {
            builder.OpenElement(100, "span");
            builder.AddAttribute(101, "class", "nt-fab-menu-item-icon");
            builder.AddAttribute(102, "aria-hidden", "true");
            builder.AddContent(103, item.Icon.Render());
            builder.CloseElement();
        }

        builder.OpenElement(110, "span");
        builder.AddAttribute(111, "class", "nt-fab-menu-item-label");
        builder.AddContent(112, item.Label);
        builder.CloseElement();
    }

    internal static IEnumerable<KeyValuePair<string, object>>? GetMenuItemAdditionalAttributes(IFabMenuItem item) {
        if (item.AdditionalAttributes is null) {
            yield break;
        }

        foreach (var attribute in item.AdditionalAttributes) {
            if (attribute.Value is not null && !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase)) {
                yield return new KeyValuePair<string, object>(attribute.Key, attribute.Value);
            }
        }
    }

    internal static bool TryGetAdditionalAttribute(IFabMenuItem item, string name, out object? value) {
        value = null;

        if (item.AdditionalAttributes is null) {
            return false;
        }

        foreach (var attribute in item.AdditionalAttributes) {
            if (string.Equals(attribute.Key, name, StringComparison.OrdinalIgnoreCase)) {
                value = attribute.Value;
                return true;
            }
        }

        return false;
    }
}
