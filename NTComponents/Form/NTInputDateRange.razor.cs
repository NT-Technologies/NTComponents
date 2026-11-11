using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;
using System.Linq.Expressions;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     A date-range field that selects a start and an end <see cref="DateOnly" /> through one read-only text field and a popover calendar.
/// </summary>
/// <remarks>
///     <para>
///         The visible field shows the range as text, and two hidden inputs carry the real <c>yyyy-MM-dd</c> values under <see cref="StartElementName" /> and <see cref="EndElementName" />.
///         The calendar picks whichever end is next: the first click sets the start, the second sets the end, and a click before the start restarts the range. Every pick writes immediately.
///     </para>
///     <para>
///         <see cref="BindAfter" /> runs once per changed end, so a pick that changes both dates invokes it twice.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a read-only text field and two hidden date inputs that post in static SSR, and adds the calendar popover with script.",
    CompatibilityDetails = "Static SSR renders the named hidden inputs and the popover markup. The browser module opens the calendar, writes both hidden values, updates the display text, and dispatches input and change events; bound state updates and live validation require an interactive render mode.")]
public partial class NTInputDateRange : IDisposable {

    private const string JsModulePath = "./_content/NTComponents/Form/NTInputDateRange.razor.js";
    private const string ValueFormat = "yyyy-MM-dd";

    private readonly string _instanceId = NTComponentIdentifier.NewId();
    private EditContext? _subscribedContext;

    /// <summary>
    ///     Gets or sets additional attributes applied to the visible text input.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the field appearance. When null, the containing <see cref="NTForm" /> appearance is used.
    /// </summary>
    [Parameter]
    public NTFormAppearance? Appearance { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked after a range value changes.
    /// </summary>
    [Parameter]
    public EventCallback BindAfter { get; set; }

    /// <summary>
    ///     Gets or sets the density. When null, the containing <see cref="NTForm" /> density is used.
    /// </summary>
    [Parameter]
    public NTFormDensity? Density { get; set; }

    /// <summary>
    ///     Gets or sets whether the field is disabled. When null, the containing <see cref="NTForm" /> state is used.
    /// </summary>
    [Parameter]
    public bool? Disabled { get; set; }

    /// <summary>
    ///     Gets or sets the format used to show each date in the text field.
    /// </summary>
    [Parameter]
    public string DisplayFormat { get; set; } = "MMM d, yyyy";

    /// <summary>
    ///     Gets or sets the end date.
    /// </summary>
    [Parameter]
    public DateOnly? EndDate { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked when the end date changes.
    /// </summary>
    [Parameter]
    public EventCallback<DateOnly?> EndDateChanged { get; set; }

    /// <summary>
    ///     Gets or sets the expression identifying the end date for validation.
    /// </summary>
    [Parameter]
    public Expression<Func<DateOnly?>>? EndDateExpression { get; set; }

    /// <summary>
    ///     Gets or sets the posted name of the end date. When null, it is derived from <see cref="EndDateExpression" />.
    /// </summary>
    [Parameter]
    public string? EndElementName { get; set; }

    /// <summary>
    ///     Gets or sets explicit error text. It takes precedence over the validation messages of both dates.
    /// </summary>
    [Parameter]
    public string? ErrorText { get; set; }

    /// <summary>
    ///     Gets or sets the visible label.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Gets or sets the last selectable date.
    /// </summary>
    [Parameter]
    public DateOnly? Max { get; set; }

    /// <summary>
    ///     Gets or sets the first selectable date.
    /// </summary>
    [Parameter]
    public DateOnly? Min { get; set; }

    /// <summary>
    ///     Gets or sets the placeholder shown while no date is selected.
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "Start date – End date";

    /// <summary>
    ///     Gets or sets whether the field is read-only. When null, the containing <see cref="NTForm" /> state is used.
    /// </summary>
    [Parameter]
    public bool? ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets the start date.
    /// </summary>
    [Parameter]
    public DateOnly? StartDate { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked when the start date changes.
    /// </summary>
    [Parameter]
    public EventCallback<DateOnly?> StartDateChanged { get; set; }

    /// <summary>
    ///     Gets or sets the expression identifying the start date for validation.
    /// </summary>
    [Parameter]
    public Expression<Func<DateOnly?>>? StartDateExpression { get; set; }

    /// <summary>
    ///     Gets or sets the posted name of the start date. When null, it is derived from <see cref="StartDateExpression" />.
    /// </summary>
    [Parameter]
    public string? StartElementName { get; set; }

    /// <summary>
    ///     Gets or sets supporting text shown when the field has no error.
    /// </summary>
    [Parameter]
    public string? SupportingText { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private NTForm? Form { get; set; }

    private string? CurrentErrorText => !string.IsNullOrWhiteSpace(ErrorText) ? ErrorText : FirstMessage(StartField) ?? FirstMessage(EndField);
    private bool IsDisabled => Disabled ?? Form?.Disabled ?? false;
    private bool IsReadOnly => ReadOnly ?? Form?.ReadOnly ?? false;
    private FieldIdentifier? EndField => EndDateExpression is null ? null : FieldIdentifier.Create(EndDateExpression);
    private FieldIdentifier? StartField => StartDateExpression is null ? null : FieldIdentifier.Create(StartDateExpression);
    private string DisplayText => StartDate is null && EndDate is null ? string.Empty : $"{Display(StartDate)} – {Display(EndDate)}".Trim();
    private int FirstDayOfWeek => (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
    private string InputId => $"{_instanceId}-input";
    private string MonthId => $"{_instanceId}-month";
    private string PopoverId => $"{_instanceId}-popover";

    /// <inheritdoc />
    public void Dispose() => Unsubscribe();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (!ReferenceEquals(_subscribedContext, EditContext)) {
            Unsubscribe();
            _subscribedContext = EditContext;
            _subscribedContext?.OnValidationStateChanged += OnValidationStateChanged;
        }
    }

    private static string? FormatValue(DateOnly? value) => value?.ToString(ValueFormat, CultureInfo.InvariantCulture);

    private string Display(DateOnly? value) => value?.ToString(DisplayFormat, CultureInfo.CurrentCulture) ?? string.Empty;

    private string? FirstMessage(FieldIdentifier? field) => field is { } id ? EditContext?.GetValidationMessages(id).FirstOrDefault() : null;

    // Rendered from C# because Razor only resolves public components and NTInputDateRangeValue is internal.
    private RenderFragment HiddenValue(DateOnly? value, Func<DateOnly?, Task> changed, Expression<Func<DateOnly?>> expression, string? name, string marker) => builder => {
        builder.OpenComponent<NTInputDateRangeValue>(0);
        builder.AddComponentParameter(1, nameof(NTInputDateRangeValue.Value), value);
        builder.AddComponentParameter(2, nameof(NTInputDateRangeValue.ValueChanged), EventCallback.Factory.Create(this, changed));
        builder.AddComponentParameter(3, nameof(NTInputDateRangeValue.ValueExpression), expression);
        builder.AddComponentParameter(4, nameof(NTInputDateRangeValue.Name), name);
        builder.AddComponentParameter(5, nameof(NTInputDateRangeValue.Disabled), IsDisabled);
        builder.AddComponentParameter(6, marker, "true");
        builder.CloseComponent();
    };

    // The hidden NTInputDateRangeValue inputs parse the posted value and notify the EditContext; these keep local state and binding in step.
    private async Task OnStartChangedAsync(DateOnly? value) {
        StartDate = value;
        await StartDateChanged.InvokeAsync(value);
        await BindAfter.InvokeAsync();
    }

    private async Task OnEndChangedAsync(DateOnly? value) {
        EndDate = value;
        await EndDateChanged.InvokeAsync(value);
        await BindAfter.InvokeAsync();
    }

    // EditContext raises this on the renderer dispatcher; rendering synchronously (as InputBase does) keeps static SSR from flushing before the error text renders.
    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs args) => StateHasChanged();

    private void Unsubscribe() {
        _subscribedContext?.OnValidationStateChanged -= OnValidationStateChanged;
        _subscribedContext = null;
    }
}
