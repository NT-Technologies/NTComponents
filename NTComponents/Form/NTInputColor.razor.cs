using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     A Material 3 aligned color field with a swatch and an NT color picker popover.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="NTInputColor" /> when a field should capture a color. The field keeps the Form text-field container, validation, density, and appearance behavior, shows the hex value as
///         editable text with a leading swatch, and opens an <see cref="NTColorPicker" /> popover from a trailing button instead of the browser's native color dialog.
///     </para>
///     <para>
///         Values are lowercase <c>#rrggbb</c> strings, or <c>#rrggbbaa</c> when <see cref="ShowAlpha" /> is enabled and opacity is below 100%. Typed or bound <c>#rgb</c>, <c>#rrggbb</c>, and
///         <c>#rrggbbaa</c> values are accepted in any case, with or without the leading <c>#</c>, and normalized. While the popover is open, changes preview live in the field. OK or clicking
///         outside keeps the change, and Cancel or Escape restores the value from when the popover opened.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a hex text field that posts in static SSR and adds the swatch preview and color picker popover with script.",
    CompatibilityDetails = "Static SSR renders a named text input that posts the hex value and the popover markup. The browser module opens the popover, previews colors live, and dispatches input and change events; bound state updates and live validation require an interactive render mode.")]
public partial class NTInputColor {

    private const string JsModulePath = "./_content/NTComponents/Form/NTInputColor.razor.js";

    private static readonly IReadOnlyDictionary<string, object?> _inputAttributes = new Dictionary<string, object?> {
        ["data-nt-input-color-input"] = "true",
        ["spellcheck"] = "false"
    };

    /// <summary>
    ///     Gets or sets a value indicating whether the picker shows an opacity slider and the value may carry an alpha channel.
    /// </summary>
    /// <remarks>
    ///     When disabled, an alpha channel in typed or bound values is dropped so the value stays <c>#rrggbb</c>.
    /// </remarks>
    [Parameter]
    public bool ShowAlpha { get; set; }

    /// <inheritdoc />
    protected override InputType InputTypeAttribute => InputType.Text;

    /// <inheritdoc />
    protected override RenderFragment? LeadingContent => _swatchFragment ??= SwatchFragment;

    /// <inheritdoc />
    protected override int? NativeMaxLength => 9;

    private readonly string _instanceId = NTComponentIdentifier.NewId();
    private string? _popoverId;
    private string? _rootStyle;
    private RenderFragment? _swatchFragment;

    private bool IsPickerDisabled => FieldDisabled || FieldReadOnly;
    private string PickerValue => TryNormalize(CurrentValueAsString, ShowAlpha, out var normalized) ? normalized : string.Empty;
    private string PopoverId => _popoverId ??= $"{_instanceId}-popover";
    private string RootClass => AppendFieldCssClass(EffectiveDensity switch {
        NTFormDensity.Comfortable => "nt-input-color nt-input-color-comfortable",
        NTFormDensity.Standard => "nt-input-color nt-input-color-standard",
        NTFormDensity.Dense => "nt-input-color nt-input-color-dense",
        var density => $"nt-input-color nt-input-color-{density.ToString().ToLowerInvariant()}"
    });
    private string RootStyle => _rootStyle ??= $"--nt-input-color-anchor: --nt-input-color-{_instanceId};";
    private string ShowAlphaAttribute => ShowAlpha ? "true" : "false";
    private string? SwatchStyle => TryNormalize(CurrentValueAsString, ShowAlpha, out var normalized) ? $"--nt-input-color-swatch: {normalized}" : null;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalInputAttributes() => _inputAttributes;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (string.IsNullOrWhiteSpace(value)) {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        if (TryNormalize(value, ShowAlpha, out var normalized)) {
            result = normalized;
            validationErrorMessage = null;
            return true;
        }

        result = null;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field must be a hex color such as #6750a4.";
        return false;
    }

    private static bool TryNormalize(string? value, bool showAlpha, [NotNullWhen(true)] out string? normalized) {
        var span = value.AsSpan().Trim();
        if (span.StartsWith('#')) {
            span = span[1..];
        }

        normalized = null;
        if (span.Length is not (3 or 6 or 8)) {
            return false;
        }

        foreach (var c in span) {
            if (!char.IsAsciiHexDigit(c)) {
                return false;
            }
        }

        if (span.Length == 8 && (!showAlpha || span[6..].Equals("ff", StringComparison.OrdinalIgnoreCase))) {
            span = span[..6];
        }

        normalized = span.Length == 3
            ? string.Create(7, span, static (chars, digits) => {
                chars[0] = '#';
                for (var i = 0; i < 3; i++) {
                    chars[1 + i * 2] = chars[2 + i * 2] = char.ToLowerInvariant(digits[i]);
                }
            })
            : string.Create(span.Length + 1, span, static (chars, digits) => {
                chars[0] = '#';
                digits.ToLowerInvariant(chars[1..]);
            });
        return true;
    }
}
