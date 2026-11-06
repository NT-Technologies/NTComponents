using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned native date/time input.
/// </summary>
/// <typeparam name="DateTimeType">The date/time value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders native date and time input markup and enhances custom picker behavior with script.",
    CompatibilityDetails = "Static SSR emits native date/time-compatible input attributes for form posts. The custom picker, live parsing, and validation updates require browser or Blazor enhancement.")]
public partial class NTInputDateTime<DateTimeType> {

    private const string JsModulePath = "./_content/NTComponents/Form/NTInputDateTime.razor.js";
    private static readonly Type _underlyingDateTimeType = Nullable.GetUnderlyingType(typeof(DateTimeType)) ?? typeof(DateTimeType);

    private IReadOnlyDictionary<string, object?>? _additionalInputAttributes;
    private string? _additionalInputAttributesFormat;
    private string? _additionalInputAttributesPickerId;
    private string? _additionalInputAttributesPickerMode;
    private bool _additionalInputAttributesCustomPicker;
    private DateTimeInputMetadata _metadata = default!;
    private bool _metadataMonthOnly;
    private bool _metadataResolved;
    private string _effectiveFormat = string.Empty;
    private NTIcon? _filledPickerIcon;
    private string? _filledPickerIconName;
    private NTColor? _filledPickerIconColor;
    private IconSize _filledPickerIconSize;
    private RenderFragment? _filledPickerIconTooltip;
    private NTIcon _resolvedPickerTriggerIcon = MaterialIcon.CalendarMonth;
    private string _pickerClass = string.Empty;
    private string _pickerHeadlineId = string.Empty;
    private string _pickerId = string.Empty;
    private bool _preserveNativeEditingBuffer;
    private bool _nativeInputFocused;
    private string _rootClass = "nt-input-date-time nt-input-date-time-standard";

    private string RootClass => AppendFieldCssClass(_rootClass);

    /// <summary>
    ///     Gets or sets a value indicating whether the Material 3 aligned custom picker should be rendered.
    /// </summary>
    /// <remarks>
    ///     When enabled, the native input still posts and binds the value while the custom picker provides the docked or
    ///     modal visual interaction. On small screens and down, the picker is presented as a centered modal dialog.
    ///     Selections write to the field immediately: date and month pickers close after a pick, date-time and time
    ///     pickers stay open for further edits, and Cancel restores the value the field had when the picker opened.
    /// </remarks>
    [Parameter]
    public bool EnableCustomPicker { get; set; }

    /// <summary>
    ///     Gets or sets the format string used to display the date/time value.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether <see cref="DateOnly" /> should render as a month input.
    /// </summary>
    [Parameter]
    public bool MonthOnly { get; set; }

    /// <summary>
    ///     Gets or sets the icon used by the custom picker trigger.
    /// </summary>
    [Parameter]
    public NTIcon? PickerTriggerIcon { get; set; }

    /// <summary>
    ///     Gets the resolved legacy input type.
    /// </summary>
    public InputType Type => _metadata.Type;

    /// <inheritdoc />
    protected override InputType InputTypeAttribute => EnableCustomPicker && !UsesNativeInputFormat() ? InputType.Text : _metadata.Type;

    /// <inheritdoc />
    protected override string? FallbackPlaceholder => EnableCustomPicker ? _effectiveFormat : null;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalInputAttributes() {
        var pickerId = BuildPickerId();
        if (_additionalInputAttributes is not null
            && _additionalInputAttributesCustomPicker == EnableCustomPicker
            && _additionalInputAttributesFormat == _effectiveFormat
            && _additionalInputAttributesPickerId == pickerId
            && _additionalInputAttributesPickerMode == _metadata.PickerMode) {
            return _additionalInputAttributes;
        }

        var attributes = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["format"] = _effectiveFormat
        };
        if (EnableCustomPicker) {
            attributes["data-tnt-dtp-input"] = "true";
            attributes["data-tnt-dtp-target"] = pickerId;
            attributes["data-tnt-dtp-mode"] = _metadata.PickerMode;
            attributes["data-tnt-dtp-open-on-focus"] = "true";
        }
        else {
            attributes["data-tnt-dtp-native-input"] = "true";
            attributes["onfocus"] = EventCallback.Factory.Create<FocusEventArgs>(this, OnNativeInputFocusAsync);
        }

        _additionalInputAttributes = attributes;
        _additionalInputAttributesCustomPicker = EnableCustomPicker;
        _additionalInputAttributesFormat = _effectiveFormat;
        _additionalInputAttributesPickerId = pickerId;
        _additionalInputAttributesPickerMode = _metadata.PickerMode;
        return _additionalInputAttributes;
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(DateTimeType? value) {
        return value switch {
            DateTime dateTimeValue => BindConverter.FormatValue(dateTimeValue, ValueFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffsetValue => BindConverter.FormatValue(dateTimeOffsetValue, ValueFormat, CultureInfo.InvariantCulture),
            DateOnly dateOnlyValue => BindConverter.FormatValue(dateOnlyValue, ValueFormat, CultureInfo.InvariantCulture),
            TimeOnly timeOnlyValue => BindConverter.FormatValue(timeOnlyValue, ValueFormat, CultureInfo.InvariantCulture),
            _ => string.Empty
        };
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        ResolveMetadata();
        _effectiveFormat = string.IsNullOrWhiteSpace(Format) ? _metadata.DefaultFormat : Format;
        base.OnParametersSet();
        CachePickerState();
    }

    /// <inheritdoc />
    protected override async Task OnInputAsync(string? newValue) {
        if (InputTypeAttribute == InputType.Text || string.IsNullOrEmpty(newValue) || !TryConvertValue(newValue, out var parsedValue)) {
            await base.OnInputAsync(newValue);
            return;
        }

        // Native date/time controls can emit a valid intermediate value while the user is still editing a segment.
        // Keep the model current, but do not let form or validation renders write into the focused native control and
        // discard the browser's editing buffer. The blur event releases one final render with the settled value.
        _preserveNativeEditingBuffer = _nativeInputFocused;
        Value = parsedValue;
        await ValueChanged.InvokeAsync(parsedValue);
        CurrentValueAsString = newValue;
        EditContext?.NotifyFieldChanged(FieldIdentifier);
        await BindAfterInputAsync();
    }

    /// <inheritdoc />
    protected override async Task OnBlurAsync(FocusEventArgs args) {
        _nativeInputFocused = false;
        _preserveNativeEditingBuffer = false;
        await base.OnBlurAsync(args);
    }

    /// <inheritdoc />
    protected override bool ShouldRender() => !_preserveNativeEditingBuffer && base.ShouldRender();

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTimeType result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (TryConvertValue(value, out result)) {
            validationErrorMessage = null;
            return true;
        }

        validationErrorMessage = $"Enter a value in the format {_effectiveFormat}.";
        return false;
    }

    private static bool TryConvertValue(string? value, out DateTimeType result) {
        object? parsedValue;
        bool converted;
        if (typeof(DateTimeType) == typeof(DateTime)) {
            converted = BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out DateTime parsed);
            parsedValue = parsed;
        }
        else if (typeof(DateTimeType) == typeof(DateTime?)) {
            converted = BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out DateTime? parsed);
            parsedValue = parsed;
        }
        else if (typeof(DateTimeType) == typeof(DateTimeOffset)) {
            converted = BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out DateTimeOffset parsed);
            parsedValue = parsed;
        }
        else if (typeof(DateTimeType) == typeof(DateTimeOffset?)) {
            converted = BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out DateTimeOffset? parsed);
            parsedValue = parsed;
        }
        else if (typeof(DateTimeType) == typeof(DateOnly)) {
            converted = BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out DateOnly parsed);
            parsedValue = parsed;
        }
        else if (typeof(DateTimeType) == typeof(DateOnly?)) {
            converted = BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out DateOnly? parsed);
            parsedValue = parsed;
        }
        else if (typeof(DateTimeType) == typeof(TimeOnly)) {
            converted = BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out TimeOnly parsed);
            parsedValue = parsed;
        }
        else if (typeof(DateTimeType) == typeof(TimeOnly?)) {
            converted = BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out TimeOnly? parsed);
            parsedValue = parsed;
        }
        else {
            result = default!;
            return false;
        }

        result = converted ? (DateTimeType)parsedValue! : default!;
        return converted;
    }

    private bool IsPickerTriggerDisabled => Disabled ?? Form?.Disabled ?? false;

    private Task OnNativeInputFocusAsync(FocusEventArgs _) {
        _nativeInputFocused = true;
        return Task.CompletedTask;
    }

    private string ValueFormat => EnableCustomPicker || UsesNativeInputFormat() ? _effectiveFormat : _metadata.DefaultFormat;

    private string BuildPickerId() => $"{EffectiveInputId}-picker";

    private bool UsesNativeInputFormat() => _metadata.Type switch {
        InputType.Date => _effectiveFormat == "yyyy-MM-dd",
        InputType.DateTime => _effectiveFormat is "yyyy-MM-ddTHH:mm" or "yyyy-MM-ddTHH:mm:ss",
        InputType.Month => _effectiveFormat == "yyyy-MM",
        InputType.Time => _effectiveFormat is "HH:mm" or "HH:mm:ss",
        _ => false
    };

    private void CachePickerState() {
        _pickerId = BuildPickerId();
        _pickerHeadlineId = $"{_pickerId}-headline";
        _pickerClass = $"tnt-dtp-overlay tnt-dtp-mode-{_metadata.PickerMode}";
        _rootClass = BuildRootClass(Density ?? Form?.Density ?? NTFormDensity.Standard);
        _resolvedPickerTriggerIcon = ResolvePickerTriggerIcon();
    }

    private string BuildRootClass(NTFormDensity density) {
        var densityClass = density switch {
            NTFormDensity.Comfortable => "nt-input-date-time-comfortable",
            NTFormDensity.Dense => "nt-input-date-time-dense",
            _ => "nt-input-date-time-standard"
        };

        var pickerClass = EnableCustomPicker ? "nt-input-date-time-custom-picker" : "nt-input-date-time-native-picker";
        return $"nt-input-date-time {pickerClass} {densityClass}";
    }

    private void ResolveMetadata() {
        var monthOnly = _underlyingDateTimeType == typeof(DateOnly) && MonthOnly;
        if (_metadataResolved && _metadataMonthOnly == monthOnly) {
            return;
        }

        _metadata = CreateMetadata(monthOnly);
        _metadataMonthOnly = monthOnly;
        _metadataResolved = true;
    }

    private static DateTimeInputMetadata CreateMetadata(bool monthOnly) {
        if (_underlyingDateTimeType == typeof(DateTime) || _underlyingDateTimeType == typeof(DateTimeOffset)) {
            return new DateTimeInputMetadata(InputType.DateTime, "yyyy-MM-ddTHH:mm:ss", "datetime", "Date and time", "Select date and time", "Open date and time picker", MaterialIcon.CalendarMonth);
        }

        if (_underlyingDateTimeType == typeof(TimeOnly)) {
            return new DateTimeInputMetadata(InputType.Time, "HH:mm:ss", "time", "Time", "Select time", "Open time picker", MaterialIcon.Schedule);
        }

        if (_underlyingDateTimeType == typeof(DateOnly)) {
            return monthOnly
                ? new DateTimeInputMetadata(InputType.Month, "yyyy-MM", "month", "Month", "Select month", "Open month picker", MaterialIcon.CalendarMonth)
                : new DateTimeInputMetadata(InputType.Date, "yyyy-MM-dd", "date", "Date", "Select date", "Open date picker", MaterialIcon.CalendarMonth);
        }

        throw new InvalidOperationException($"The type '{typeof(DateTimeType)}' is not a supported DateTime type.");
    }

    private NTIcon ResolvePickerTriggerIcon() {
        var pickerIcon = PickerTriggerIcon ?? _metadata.DefaultPickerIcon;
        if (pickerIcon is not MaterialIcon materialIcon) {
            return pickerIcon;
        }

        if (_filledPickerIcon is not null
            && string.Equals(_filledPickerIconName, materialIcon.Icon, StringComparison.Ordinal)
            && _filledPickerIconColor == materialIcon.Color
            && _filledPickerIconSize == materialIcon.Size
            && ReferenceEquals(_filledPickerIconTooltip, materialIcon.Tooltip)) {
            return _filledPickerIcon;
        }

        _filledPickerIcon = new MaterialIcon(materialIcon.Icon) {
            Appearance = IconAppearance.Filled,
            Color = materialIcon.Color,
            Size = materialIcon.Size,
            Tooltip = materialIcon.Tooltip
        };
        _filledPickerIconName = materialIcon.Icon;
        _filledPickerIconColor = materialIcon.Color;
        _filledPickerIconSize = materialIcon.Size;
        _filledPickerIconTooltip = materialIcon.Tooltip;
        return _filledPickerIcon;
    }

    private sealed record DateTimeInputMetadata(InputType Type, string DefaultFormat, string PickerMode, string PickerHeadline, string PickerSupportingLabel, string PickerTriggerAriaLabel, NTIcon DefaultPickerIcon);

}
