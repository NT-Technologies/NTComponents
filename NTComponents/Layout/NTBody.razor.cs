using Microsoft.AspNetCore.Components;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 body region for page or nested layout content inside an <see cref="NTLayout" />.
/// </summary>
/// <remarks>
///     <para>Defaults to <see cref="NTLayoutTag.Main" />. Set <see cref="NTComponents.Layout.NTLayoutComponentBase.Tag" /> to a neutral or sectioning tag when nesting.</para>
///     <para>
///         The body is the page's scroll container, so the browser's own scrolling on navigation never reaches it. By default the body does that
///         itself: a new page starts at the top, or at the element named by the URL fragment, and Back or Forward returns to where the page was
///         left in browsers with the Navigation API. Set <see cref="KeepScrollPositionOnNavigation" /> to keep the scroll position instead.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR preserves the component structure, styling, and accessibility semantics. Dynamic parameter changes require a new render. A small browser script resets the scroll position after interactive and enhanced navigation.")]
public partial class NTBody {

    private const string JsModulePath = "./_content/NTComponents/Layout/NTBody.razor.js";

    /// <summary>
    ///     Keeps the body's scroll position when the app navigates to another page, instead of starting the new page at the top or at its URL
    ///     fragment.
    /// </summary>
    [Parameter]
    public bool KeepScrollPositionOnNavigation { get; set; }

    private RenderFragment Content => builder => {
        if (!KeepScrollPositionOnNavigation) {
            builder.OpenComponent<NTPageScript>(0);
            builder.AddAttribute(1, nameof(NTPageScript.Src), JsModulePath);
            builder.CloseComponent();
        }

        builder.AddContent(2, ChildContent);
    };

    /// <summary>
    ///     Enables rounded corners on the body scroll container.
    /// </summary>
    [Parameter]
    public bool RoundedCorners { get; set; } = true;

    /// <inheritdoc />
    protected override string ComponentClass => "nt-body";

    /// <inheritdoc />
    protected override string? ComponentStateClass => RoundedCorners ? "nt-body-rounded" : null;

    /// <inheritdoc />
    protected override NTLayoutTag DefaultTag => NTLayoutTag.Main;

}
