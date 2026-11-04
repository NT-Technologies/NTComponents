using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NTComponents;

/// <summary>
///     One hidden <c>yyyy-MM-dd</c> value of an <see cref="NTInputDateRange" />.
/// </summary>
/// <remarks>
///     Deriving from <see cref="InputBase{TValue}" /> gives the hidden input Blazor's own form-post name, so static SSR model binding,
///     nested editor prefixes, parsing, and field-change notification all match the other form inputs.
/// </remarks>
internal sealed class NTInputDateRangeValue : InputBase<DateOnly?> {

    private const string ValueFormat = "yyyy-MM-dd";

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Name { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "hidden");
        builder.AddAttribute(3, "name", Name ?? NameAttributeValue);
        builder.AddAttribute(4, "value", CurrentValueAsString);
        builder.AddAttribute(5, "disabled", Disabled);
        builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string?>(this, value => CurrentValueAsString = value, CurrentValueAsString));
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(DateOnly? value) => value?.ToString(ValueFormat, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out DateOnly? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        var parsed = DateOnly.TryParseExact(value, ValueFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
        result = parsed ? date : null;
        validationErrorMessage = parsed ? null : $"The {FieldIdentifier.FieldName} field must be a date.";
        return parsed;
    }
}
