using Microsoft.AspNetCore.Components;
using NTComponents.Interfaces;

namespace NTComponents.Core;

/// <summary>
///     Base component for all NTComponents.
/// </summary>
/// <seealso cref="ComponentBase" />
/// <seealso cref="INTComponentBase" />
/// <seealso cref="IAsyncDisposable" />
public abstract class NTComponentBase : ComponentBase, INTComponentBase {

    /// <inheritdoc />
    [Parameter(CaptureUnmatchedValues = true)]
    public virtual IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool? AutoFocus { get; set; }

    /// <summary>
    ///     Unique identifier for the component.
    /// </summary>
    public string ComponentIdentifier { get; } = NTComponentIdentifier.NewId();

    /// <inheritdoc />
    public ElementReference Element { get; protected set; }

    /// <inheritdoc />
    public abstract string? ElementClass { get; }

    /// <inheritdoc />
    [Parameter]
    public virtual string? ElementId { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementLang { get; set; }

    /// <inheritdoc />
    public abstract string? ElementStyle { get; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <summary>
    ///     Custom identifier attribute for the component.
    /// </summary>
    internal const string _tnTCustomIdentifierAttribute = "tntid";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        // Copy only when the identifier is missing or stale; the incoming dictionary belongs to the parent.
        if (AdditionalAttributes?.TryGetValue(_tnTCustomIdentifierAttribute, out var identifier) == true && Equals(identifier, ComponentIdentifier)) {
            return;
        }

        AdditionalAttributes = AdditionalAttributes is null
            ? new Dictionary<string, object>(1) { [_tnTCustomIdentifierAttribute] = ComponentIdentifier }
            : new Dictionary<string, object>(AdditionalAttributes) { [_tnTCustomIdentifierAttribute] = ComponentIdentifier };
    }
}
