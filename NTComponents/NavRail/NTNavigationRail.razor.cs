using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 navigation rail for top-level app destinations.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="NTNavigationRail" /> renders a semantic <c>nav</c> landmark. The default expand/collapse affordance is a native button progressively enhanced by the component's browser module;
///         destination items remain plain links or native buttons.
///     </para>
///     <para>
///         The rail renders a collapsed static shell by default and the browser module owns expansion after initialization. Use <see cref="RailCollapseBehavior.Hide" /> when the collapsed state
///         should use an external trigger and temporary modal surface.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a semantic nav landmark and progressively enhances rail expansion.",
    CompatibilityDetails = "Destination links remain usable in static SSR. The browser module enhances the collapse and modal rail affordances when JavaScript is available.")]
public partial class NTNavigationRail {

    /// <summary>
    ///     Accessible label for the rail landmark.
    /// </summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    /// <summary>
    ///     Rail content, normally <see cref="NTNavigationRailItem" /> children.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Whether selecting an enabled destination link closes its collapsed-rail group popover and the modal rail. Defaults to <see langword="true" />.
    /// </summary>
    [Parameter]
    public bool CloseOnNavigation { get; set; } = true;

    /// <summary>
    ///     Behavior represented when the rail is not expanded.
    /// </summary>
    [Parameter]
    public RailCollapseBehavior CollapseBehavior { get; set; }

    /// <summary>
    ///     Whether the rail uses reduced vertical spacing and one-stage-smaller destination typography.
    /// </summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>
    ///     Rail container color.
    /// </summary>
    [Parameter]
    public NTColor? ContainerColor { get; set; }

    /// <summary>
    ///     Color used by the optional content-adjacent divider.
    /// </summary>
    [Parameter]
    public NTColor? DividerColor { get; set; }

    /// <summary>
    ///     Optional elevation class applied to the rail surface.
    /// </summary>
    [Parameter]
    public NTElevation? Elevation { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => _elementClass;

    /// <inheritdoc />
    public override string? ElementStyle => _elementStyle;

    /// <summary>
    ///     Browser module that progressively enhances the default menu button into an in-place expand/collapse control.
    /// </summary>
    public override string? JsModulePath => "./_content/NTComponents/NavRail/NTNavigationRail.razor.js";

    /// <summary>
    ///     Optional FAB or extended FAB content rendered above destinations.
    /// </summary>
    [Parameter]
    public RenderFragment? Fab { get; set; }

    /// <summary>
    ///     Optional footer content. Use only when the app shell has a concrete need for lower rail content.
    /// </summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    ///     Optional header/logo content rendered above destinations.
    /// </summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>
    ///     Hides the collapsed rail on extra-small screens, and on small screens in portrait orientation, and renders only a top-edge menu button that opens the modal rail.
    /// </summary>
    [Parameter]
    public bool HideRailOnXSScreens { get; set; } = true;

    /// <summary>
    ///     Active indicator width treatment.
    /// </summary>
    [Parameter]
    public ActiveLinkIndicatorStyle IndicatorStyle { get; set; }

    /// <summary>
    ///     Whether expanding a top-level group closes all other expanded top-level groups. Nested groups are unaffected.
    /// </summary>
    [Parameter]
    public bool LimitToOneExpanded { get; set; }

    /// <summary>
    ///     Optional override for the selected destination icon and label color.
    /// </summary>
    [Parameter]
    public NTColor? ActiveColor { get; set; }

    /// <summary>
    ///     Optional override for the active destination indicator color.
    /// </summary>
    [Parameter]
    public NTColor? IndicatorColor { get; set; }

    /// <summary>
    ///     Optional override for the modal scrim color. Defaults to the Material 3 scrim color role.
    /// </summary>
    [Parameter]
    public NTColor? ScrimColor { get; set; }

    /// <summary>
    ///     Optional override for hover, focus, pressed, and ripple state-layer color.
    /// </summary>
    [Parameter]
    public NTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Optional custom menu/collapse affordance content.
    /// </summary>
    [Parameter]
    public RenderFragment? Menu { get; set; }

    /// <summary>
    ///     Accessible label for the default menu button while the rail is collapsed.
    /// </summary>
    [Parameter]
    public string MenuAriaLabel { get; set; } = "Expand navigation rail";

    /// <summary>
    ///     Accessible label for the default menu button after the rail is expanded.
    /// </summary>
    [Parameter]
    public string MenuAriaLabelWhenExpanded { get; set; } = "Collapse navigation rail";

    /// <summary>
    ///     Legacy opt-in for the default menu affordance. The rendered affordance is a button and does not navigate.
    /// </summary>
    [Parameter]
    public string? MenuHref { get; set; }

    /// <summary>
    ///     Optional menu icon override for the default menu affordance.
    /// </summary>
    [Parameter]
    public NTIcon? MenuIcon { get; set; }

    /// <summary>
    ///     Opens the navigation rail by default on initial load for medium and larger screens only.
    /// </summary>
    [Parameter]
    public bool OpenByDefault { get; set; }

    /// <summary>
    ///     Optional expanded-state menu icon override for the default menu affordance.
    /// </summary>
    [Parameter]
    public NTIcon? MenuIconWhenExpanded { get; set; }

    /// <summary>
    ///     Whether the optional divider should be rendered on the content-adjacent edge.
    /// </summary>
    [Parameter]
    public bool ShowDivider { get; set; } = true;

    /// <summary>
    ///     Visual shape treatment for active destination indicators.
    /// </summary>
    [Parameter]
    public NTNavigationRailVariant Variant { get; set; } = NTNavigationRailVariant.Pill;

    internal string EffectiveElementId => _effectiveElementId;
    internal string ExternalMenuButtonId => _externalMenuButtonId;
    internal NTIcon EffectiveCollapsedMenuIcon => MenuIcon ?? MaterialIcon.Menu;
    internal NTIcon EffectiveExpandedMenuIcon => MenuIconWhenExpanded ?? MenuIcon ?? MaterialIcon.MenuOpen;
    internal NTIcon EffectiveMenuIcon => IsExpanded ? EffectiveExpandedMenuIcon : EffectiveCollapsedMenuIcon;
    internal string ExternalMenuButtonClass => _externalMenuButtonClass;
    internal bool IsExpanded => OpenByDefault;
    internal string? IsExpandedText => _isExpandedText;
    internal string LimitToOneExpandedText => LimitToOneExpanded ? "true" : "false";
    internal string OpenByDefaultText => _openByDefaultText;
    internal bool ShouldRenderExternalMenuButton => HideRailOnXSScreens || CollapseBehavior == RailCollapseBehavior.Hide;
    private string? _elementClass;
    private string? _elementStyle;
    private string _effectiveElementId = string.Empty;
    private string _externalMenuButtonId = string.Empty;
    private string _externalMenuButtonClass = string.Empty;
    private string _isExpandedText = "false";
    private string _openByDefaultText = "false";
    private string _stableElementId = string.Empty;
    private bool ShouldRenderDefaultMenuButton => _shouldRenderDefaultMenuButton;
    private bool _shouldRenderDefaultMenuButton;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (string.IsNullOrWhiteSpace(AriaLabel)) {
            throw new ArgumentException("NTNavigationRail requires a non-empty AriaLabel.", nameof(AriaLabel));
        }

        _stableElementId = CreateStableElementId(AriaLabel);

        ContainerColor ??= NTColor.Surface;
        DividerColor ??= NTColor.OutlineVariant;
        IndicatorColor ??= NTColor.SecondaryContainer;
        ActiveColor ??= NTColor.OnSecondaryContainer;
        ScrimColor ??= NTColor.Scrim;
        StateLayerColor ??= NTColor.OnSecondaryContainer;

        _effectiveElementId = ElementId ?? TryGetStringAttribute("id") ?? _stableElementId;
        _externalMenuButtonId = $"{_effectiveElementId}-xs-menu-button";
        _externalMenuButtonClass = CssClassBuilder.Create()
            .AddClass("nt-navigation-rail-xs-menu-button")
            .AddClass("nt-navigation-rail-hide-menu-button", CollapseBehavior == RailCollapseBehavior.Hide)
            .AddClass("nt-navigation-rail-menu-button")
            .Build() ?? string.Empty;
        _isExpandedText = IsExpanded ? "true" : "false";
        _openByDefaultText = OpenByDefault ? "true" : "false";
        _shouldRenderDefaultMenuButton = ShouldRenderExternalMenuButton || !string.IsNullOrWhiteSpace(MenuHref);
        _elementClass = BuildElementClass();
        _elementStyle = BuildElementStyle();
    }

    private string? BuildElementClass() => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-navigation-rail")
        .AddClass("nt-navigation-rail-compact", Compact)
        .AddClass("nt-navigation-rail-collapsed", !IsExpanded)
        .AddClass("nt-navigation-rail-expanded", IsExpanded)
        .AddClass("nt-navigation-rail-hide-on-xs", HideRailOnXSScreens)
        .AddClass("nt-navigation-rail-hide-when-collapsed", CollapseBehavior == RailCollapseBehavior.Hide)
        .AddClass("nt-navigation-rail-with-divider", when: ShowDivider)
        .AddClass("nt-navigation-rail-with-header", Header is not null)
        .AddClass("nt-navigation-rail-with-fab", Fab is not null)
        .AddClass("nt-navigation-rail-indicator-full", IndicatorStyle == ActiveLinkIndicatorStyle.FullWidth)
        .AddClass("nt-navigation-rail-square", Variant == NTNavigationRailVariant.Square)
        .AddElevation(Elevation)
        .Build();

    private string? BuildElementStyle() => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-navigation-rail-container-color", ContainerColor.ToCssNTColorVariable(), ContainerColor.HasValue)
        .AddVariable("nt-navigation-rail-divider-color", DividerColor.ToCssNTColorVariable(), (ShowDivider || Header is not null || Footer is not null) && DividerColor.HasValue)
        .AddVariable("nt-navigation-rail-indicator-color", IndicatorColor.ToCssNTColorVariable(), IndicatorColor.HasValue)
        .AddVariable("nt-navigation-rail-active-color", ActiveColor.ToCssNTColorVariable(), ActiveColor.HasValue)
        .AddVariable("nt-navigation-rail-scrim-color", ScrimColor.ToCssNTColorVariable(), ScrimColor.HasValue)
        .AddVariable("nt-navigation-rail-state-layer-color", StateLayerColor.ToCssNTColorVariable(), StateLayerColor.HasValue)
        .Build();

    private string? TryGetStringAttribute(string attributeName) {
        return AdditionalAttributes?.TryGetValue(attributeName, out var value) == true
            ? Convert.ToString(value)
            : null;
    }

    private static string CreateStableElementId(string value) {
        Span<char> buffer = stackalloc char[Math.Min(value.Length, 48)];
        var index = 0;
        var previousWasSeparator = false;

        foreach (var character in value.Trim()) {
            if (index >= buffer.Length) {
                break;
            }

            if (char.IsAsciiLetterOrDigit(character)) {
                buffer[index++] = char.ToLowerInvariant(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && index > 0) {
                buffer[index++] = '-';
                previousWasSeparator = true;
            }
        }

        if (index > 0 && buffer[index - 1] == '-') {
            index--;
        }

        return index == 0
            ? NTComponentIdentifier.NewId()
            : $"nt-navigation-rail-{buffer[..index]}";
    }
}
