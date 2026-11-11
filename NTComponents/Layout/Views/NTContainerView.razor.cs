using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
/// Centered page-width container view for readable content regions.
/// </summary>
/// <remarks>
/// <para> Use this view when page content should stay centered and bounded instead of stretching across the full
/// viewport. It provides Material 3 window-size breakpoints.
/// </para> <para> Best practice: use this as a layout wrapper around page sections, forms, articles, or view
/// compositions that need comfortable reading width. Keep shape, color, elevation, and surface styling on the content
/// inside the container. </para> <para> Do not use this view to create cards, panels, or visual surfaces. It controls
/// only horizontal placement and width.</para>
/// <para>The "On this page" navigation finds headings (<c>h1</c> through <c>h6</c>) in the container. Set
/// <c>data-nt-nav-label</c> on a heading to change its navigation link text, or <c>data-nt-nav-exclude</c> on a heading
/// or any element containing headings to omit their links. The nearest element with the attribute wins, and a value of
/// <c>false</c> includes the heading, so a heading inside an excluded region can opt back in.</para>
/// <code>
/// &lt;h2 data-nt-nav-label="Summary"&gt;Overview and scope&lt;/h2&gt;
/// &lt;h2 data-nt-nav-exclude&gt;Internal details&lt;/h2&gt;
/// &lt;aside data-nt-nav-exclude&gt;&lt;h3&gt;Related links&lt;/h3&gt;&lt;/aside&gt;
/// </code>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders useful static markup and enhances behavior with browser JavaScript.",
    CompatibilityDetails = "Static SSR emits the component shell and accessible markup. The browser module adds richer behavior after the page reaches the browser.")]
public partial class NTContainerView {

    /// <inheritdoc />
    public override string? JsModulePath => ContainerViewJsModulePath;

    /// <summary>
    ///     Content rendered inside the centered container.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Whether the container should render and hydrate the generated "On this page" navigation.
    /// </summary>
    /// <remarks>Headings can set <c>data-nt-nav-label</c> to override their link text. <c>data-nt-nav-exclude</c> on a heading or a containing element omits the links.</remarks>
    [Parameter]
    public bool EnableOnThisPageNavigation { get; set; } = true;

    /// <summary>
    ///     Label rendered above the generated on-this-page links.
    /// </summary>
    [Parameter]
    public string OnThisPageLabel { get; set; } = "On this page";

    /// <summary>
    ///     Accessible label for the generated on-this-page navigation landmark. Defaults to <see cref="OnThisPageLabel" />.
    /// </summary>
    [Parameter]
    public string? OnThisPageAriaLabel { get; set; }

    /// <summary>
    ///     Optional title rendered below the "On this page" label.
    /// </summary>
    [Parameter]
    public string? OnThisPageTitle { get; set; }

    /// <summary>
    ///     Optional color override for the "On this page" label.
    /// </summary>
    [Parameter]
    public NTColor? OnThisPageLabelColor { get; set; } = NTColor.OnSurfaceVariant;

    /// <summary>
    ///     Optional color override for the quick navigation selector outline.
    /// </summary>
    [Parameter]
    public NTColor? OnThisPageSelectorColor { get; set; } = NTColor.Outline;

    /// <summary>
    ///     Optional color override for the selected quick navigation link text.
    /// </summary>
    [Parameter]
    public NTColor? OnThisPageSelectedTextColor { get; set; } = NTColor.OnSurface;

    /// <summary>
    ///     Optional color override for quick navigation link text.
    /// </summary>
    [Parameter]
    public NTColor? OnThisPageTextColor { get; set; } = NTColor.OnSurface;

    /// <inheritdoc />
    protected override string ComponentClass => "nt-container-view";

    /// <inheritdoc />
    protected override bool ShouldLoadJsModule => EnableOnThisPageNavigation;

    private bool HasOnThisPageTitle => !string.IsNullOrWhiteSpace(OnThisPageTitle);

    private string OnThisPageNavigationAriaLabel => string.IsNullOrWhiteSpace(OnThisPageAriaLabel) ? OnThisPageLabel : OnThisPageAriaLabel;

    private string? OnThisPageNavigationEnabledAttribute => EnableOnThisPageNavigation ? "true" : null;

    private const string ContainerViewJsModulePath = "./_content/NTComponents/Layout/Views/NTContainerView.razor.js";

    /// <inheritdoc />
    protected override void AddComponentStyles(CssStyleBuilder builder) {
        builder
            .AddVariable("nt-container-view-on-this-page-label-color", OnThisPageLabelColor.ToCssNTColorVariable(), OnThisPageLabelColor.HasValue)
            .AddVariable("nt-container-view-on-this-page-selector-color", OnThisPageSelectorColor.ToCssNTColorVariable(), OnThisPageSelectorColor.HasValue)
            .AddVariable("nt-container-view-on-this-page-selected-text-color", OnThisPageSelectedTextColor.ToCssNTColorVariable(), OnThisPageSelectedTextColor.HasValue)
            .AddVariable("nt-container-view-on-this-page-text-color", OnThisPageTextColor.ToCssNTColorVariable(), OnThisPageTextColor.HasValue);
    }
}
