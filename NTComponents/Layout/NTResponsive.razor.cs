using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Shows or hides content on either side of a viewport-width breakpoint.
/// </summary>
/// <remarks>
///     <para>
///         The wrapper uses <c>display: contents</c> while visible so it does not introduce a layout box. The <see cref="Direction" /> value <see cref="NTBreakpointDirection.Above" /> includes
///         the breakpoint width; <see cref="NTBreakpointDirection.Below" /> applies only below it.
///     </para>
///     <para>
///         Set <see cref="CustomBreakpoint" /> to override the selected Material 3 <see cref="Breakpoint" /> with an exact pixel width.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders responsive visibility rules without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR emits the content and viewport media rules. Dynamic parameter changes require a new render.")]
public partial class NTResponsive {

    /// <summary>
    ///     Material 3 viewport-width threshold used when <see cref="CustomBreakpoint" /> is not set.
    /// </summary>
    [Parameter]
    public NTBreakpoint Breakpoint { get; set; } = NTBreakpoint.Medium;

    /// <summary>
    ///     Content controlled by the responsive visibility rule.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Optional viewport-width threshold in pixels. When set, this value overrides <see cref="Breakpoint" />.
    /// </summary>
    [Parameter]
    public int? CustomBreakpoint { get; set; }

    /// <summary>
    ///     Side of the breakpoint targeted by <see cref="Visibility" />. <see cref="NTBreakpointDirection.Above" /> includes the exact breakpoint width.
    /// </summary>
    [Parameter]
    public NTBreakpointDirection Direction { get; set; } = NTBreakpointDirection.Above;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-responsive")
        .AddClass($"nt-responsive-visible-{VisibleDirection}")
        .AddClass(CustomBreakpoint.HasValue ? "nt-responsive-custom" : $"nt-responsive-{BreakpointName}")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Whether content is shown or hidden on the targeted side of the breakpoint.
    /// </summary>
    [Parameter]
    public NTBreakpointVisibility Visibility { get; set; } = NTBreakpointVisibility.Show;

    private string BreakpointAttribute => CustomBreakpoint?.ToString() ?? BreakpointName;

    private string BreakpointName => Breakpoint switch {
        NTBreakpoint.Small => "small",
        NTBreakpoint.Medium => "medium",
        NTBreakpoint.Large => "large",
        NTBreakpoint.ExtraLarge => "extra-large",
        _ => throw new ArgumentOutOfRangeException(nameof(Breakpoint), Breakpoint, "The breakpoint must be a defined value.")
    };

    private string CustomMediaQuery => $"(min-width: {CustomBreakpoint}px)";

    private string CustomMediaRule => $".nt-responsive.nt-responsive-custom.nt-responsive-visible-{VisibleDirection}[ntid=\"{ComponentIdentifier}\"]{{display:{CustomDisplay}}}";

    private string CustomDisplay => VisibleDirection == "above" ? "contents" : "none";

    private string DirectionAttribute => Direction switch {
        NTBreakpointDirection.Above => "above",
        NTBreakpointDirection.Below => "below",
        _ => throw new ArgumentOutOfRangeException(nameof(Direction), Direction, "The breakpoint direction must be a defined value.")
    };

    private string VisibilityAttribute => Visibility switch {
        NTBreakpointVisibility.Show => "show",
        NTBreakpointVisibility.Hide => "hide",
        _ => throw new ArgumentOutOfRangeException(nameof(Visibility), Visibility, "The breakpoint visibility must be a defined value.")
    };

    private string VisibleDirection => Visibility switch {
        NTBreakpointVisibility.Show => DirectionAttribute,
        NTBreakpointVisibility.Hide => Direction == NTBreakpointDirection.Above ? "below" : "above",
        _ => throw new ArgumentOutOfRangeException(nameof(Visibility), Visibility, "The breakpoint visibility must be a defined value.")
    };

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (CustomBreakpoint < 0) {
            throw new ArgumentOutOfRangeException(nameof(CustomBreakpoint), CustomBreakpoint, "The custom breakpoint must be zero or greater.");
        }

        _ = BreakpointName;
        _ = DirectionAttribute;
        _ = VisibilityAttribute;

        base.OnParametersSet();
    }
}
