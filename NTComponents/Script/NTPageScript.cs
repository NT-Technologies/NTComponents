using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.CodeDocumentation;

namespace NTComponents;

/// <summary>
///     A Blazor component that renders a custom script element with a specified source.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a static page-script marker that the browser-side loader enhances.",
    CompatibilityDetails = "Static SSR emits the custom tnt-page-script element with the requested source. The browser-side loader consumes that marker to load component scripts after the page reaches the browser.")]
public class NTPageScript : ComponentBase {

    /// <summary>
    ///     Gets or sets the source URL of the script.
    /// </summary>
    [Parameter, EditorRequired]
    public string Src { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        builder.OpenElement(0, "tnt-page-script");
        builder.AddAttribute(1, "src", Src);
        builder.CloseElement();
    }
}
