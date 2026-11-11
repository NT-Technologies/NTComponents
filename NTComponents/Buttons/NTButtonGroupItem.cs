using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Represents a button description that registers itself with <see cref="NTButtonGroup{TObjectType}" />.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The item contributes metadata to NTButtonGroup, but selection behavior depends on the interactive parent group.")]
public sealed partial class NTButtonGroupItem<TObjectType> : Microsoft.AspNetCore.Components.IComponent, IDisposable {
    private NTButtonGroup<TObjectType>? _registeredParent;

    /// <summary>
    ///     Captures unmatched attributes passed to the item.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, object?>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Provides an accessible label for icon-only items.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     The parent button group that owns this item.
    /// </summary>
    [CascadingParameter]
    public NTButtonGroup<TObjectType>? Parent { get; set; }

    /// <summary>
    ///     A unique key used to identify the item inside the group.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public TObjectType Key { get; set; } = default!;

    /// <summary>
    ///     Optional label text; when unset, the button may render as an icon-only control.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Optional icon rendered before the label.
    /// </summary>
    [Parameter]
    public NTIcon? Icon { get; set; }

    /// <summary>
    ///     Marks the item as disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Indicates whether this item should be selected by default when no other selection exists.
    /// </summary>
    [Parameter]
    public bool IsDefaultSelected { get; set; }

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) { }

    /// <inheritdoc />
    public void Dispose() {
        _registeredParent?.UnregisterItem(this);
        _registeredParent = null;
    }

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{nameof(NTButtonGroupItem<TObjectType>)} must be used inside an {nameof(NTButtonGroup<TObjectType>)}.");
        }

        if (Icon is not null && string.IsNullOrWhiteSpace(Label) && string.IsNullOrWhiteSpace(AriaLabel)) {
            throw new InvalidOperationException($"{nameof(NTButtonGroupItem<TObjectType>)} requires {nameof(AriaLabel)} when rendering an icon-only item.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterItem(this);
            Parent.RegisterItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
