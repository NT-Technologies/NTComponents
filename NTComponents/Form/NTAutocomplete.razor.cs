using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned autocomplete text field for a string value.
/// </summary>
/// <remarks>
///     Use <see cref="NTAutocomplete" /> when users enter text and can benefit from local suggestions. The bound and submitted
///     value is the native input value; <see cref="NTAutocompleteOption.Label" /> only controls menu display. Suggestions are
///     declared in <see cref="ChildContent" /> with <see cref="NTAutocompleteOption" /> and optional
///     <see cref="NTAutocompleteOptionGroup" /> children.
///     <para>
///         The component renders a native text input plus inert JSON option metadata during static SSR. The browser module
///         progressively enhances that markup into a Material-style suggestion menu, filters options with contains matching,
///         prioritizes starts-with matches, and keeps keyboard, touched-state, and selected-value updates synchronized.
///     </para>
///     <para>
///         Best practices:
///     </para>
///     <list type="bullet">
///         <item>Use this component for freeform or strict text entry with suggestions. Use <see cref="NTSelect{TValue}" /> when users must choose one known value with native browser behavior.</item>
///         <item>Use <see cref="NTCombobox{TValue}" /> when users need to select multiple values.</item>
///         <item>Keep options bounded, stable, and already available in the rendered child content. This component does not perform remote lookup.</item>
///         <item>Set <see cref="AllowCustomValue" /> to <see langword="false" /> when the submitted value must match an enabled suggestion value exactly.</item>
///         <item>Group related suggestions with <see cref="NTAutocompleteOptionGroup" /> when the list benefits from select-like section labels.</item>
///         <item>Keep labels concise. Use <see cref="NTAutocompleteOption.SupportingText" /> only when it helps distinguish similar suggestions.</item>
///         <item>Keep values stable because the value is inserted into and posted by the native input.</item>
///         <item>Provide a visible label whenever possible. If a label is intentionally hidden, provide an <c>aria-label</c> through additional attributes.</item>
///     </list>
///     <example>
///         <code>
/// &lt;NTAutocomplete @bind-Value="_city" Label="City" AllowCustomValue="false"&gt;
///     &lt;NTAutocompleteOptionGroup Label="Texas"&gt;
///         &lt;NTAutocompleteOption Value="Austin" Label="Austin" /&gt;
///         &lt;NTAutocompleteOption Value="Dallas" Label="Dallas" /&gt;
///     &lt;/NTAutocompleteOptionGroup&gt;
///     &lt;NTAutocompleteOption Value="Denver" Label="Denver" SupportingText="Colorado" /&gt;
/// &lt;/NTAutocomplete&gt;
///         </code>
///     </example>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a native text input and enhances local suggestions with JavaScript.",
    CompatibilityDetails = "Static SSR emits the named input, validation attributes, and suggestion metadata. The browser module adds the suggestion popover, filtering, keyboard navigation, and touched-state synchronization.")]
public partial class NTAutocomplete : IAsyncDisposable {
    private const string AutocompleteControlBaseClass = "nt-input-control nt-autocomplete-control";
    private const string JsModulePath = "./_content/NTComponents/Form/NTAutocomplete.razor.js";
    private static readonly HashSet<string> AutocompleteExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "name",
        "type",
        "title",
        "autofocus",
        "autocomplete",
        "readonly",
        "disabled",
        "required",
        "placeholder",
        "aria-autocomplete",
        "aria-controls",
        "aria-describedby",
        "aria-expanded",
        "aria-haspopup",
        "aria-invalid",
        "aria-errormessage",
        "aria-activedescendant",
        "role",
        "value",
        "onchange",
        "oninput"
    };

    private readonly HashSet<NTAutocompleteOption> _optionChildren = [];
    private readonly NTDisposalState _disposalState = new();
    private NTMenuFieldInterop<NTAutocomplete>? _interop;
    private bool _hasRendered;
    private bool _optionChildrenChangedBeforeFirstRender;
    private string? _customOptionJson;
    private string? _customOptionJsonFormat;
    private bool _customOptionJsonDisabled;

    /// <summary>
    ///     Gets or sets a value indicating whether users can submit values that are not declared as child <see cref="NTAutocompleteOption" /> entries.
    /// </summary>
    /// <remarks>
    ///     When this is <see langword="false" />, the component rejects non-empty values that do not match an enabled registered option value exactly after child options have registered. The browser enhancement also hides the explicit custom-value row. Option values are not written to the native <c>pattern</c> attribute.
    /// </remarks>
    [Parameter]
    public bool AllowCustomValue { get; set; } = true;

    /// <summary>
    ///     Gets or sets the suggestion options and option groups.
    /// </summary>
    /// <remarks>
    ///     Add <see cref="NTAutocompleteOption" /> entries directly for flat lists, or place them inside
    ///     <see cref="NTAutocompleteOptionGroup" /> to render grouped menu sections. The child components emit inert metadata
    ///     for static SSR and are converted into menu items by the browser enhancement.
    /// </remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the display format for the custom-value option. Use <c>{0}</c> for the typed value.
    /// </summary>
    /// <remarks>
    ///     This text is used only when <see cref="AllowCustomValue" /> is <see langword="true" /> and the current typed value is
    ///     not an exact suggestion value. If the format omits <c>{0}</c>, the text is rendered unchanged.
    /// </remarks>
    [Parameter]
    public string CustomValueOptionFormat { get; set; } = "Use \"{0}\"";

    /// <summary>
    ///     Gets or sets the text rendered when filtering produces no suggestions.
    /// </summary>
    /// <remarks>
    ///     Keep this text short and neutral. Empty filtered results are usually a normal search state, not a validation error.
    /// </remarks>
    [Parameter]
    public string EmptyText { get; set; } = "No options";

    /// <summary>
    ///     Gets or sets the popup menu item appearance.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="NTMenuItemAppearance.Condensed" /> only for compact, highly scannable suggestion lists. Condensed
    ///     menu items are capped at 38 CSS pixels and are best suited to short labels without supporting text.
    /// </remarks>
    [Parameter]
    public NTMenuItemAppearance MenuItemAppearance { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => AutocompleteExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override bool HasFloatingValue => !string.IsNullOrEmpty(CurrentValueAsString);

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-autocomplete";

    private string AllowCustomValueAttribute => AllowCustomValue ? "true" : "false";

    private string AutocompleteControlClass {
        get {
            var cssClass = CssClass;
            return string.IsNullOrEmpty(cssClass) ? AutocompleteControlBaseClass : $"{AutocompleteControlBaseClass} {cssClass}";
        }
    }

    private string AutocompleteCustomOptionJson {
        get {
            var format = CustomValueOptionFormat ?? "Use \"{0}\"";
            var disabled = FieldDisabled || FieldReadOnly;
            if (_customOptionJson is null || _customOptionJsonDisabled != disabled || !string.Equals(_customOptionJsonFormat, format, StringComparison.Ordinal)) {
                _customOptionJson = BuildCustomOptionJson(format, disabled);
                _customOptionJsonDisabled = disabled;
                _customOptionJsonFormat = format;
            }

            return _customOptionJson;
        }
    }

    private NTMenuFieldInterop<NTAutocomplete> Interop => _interop ??= new(this, JsModulePath, _disposalState, true, true);

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private string ListboxId => $"{InputId}-listbox";
    private string ListboxLabel => string.IsNullOrWhiteSpace(Label) ? FieldIdentifier.FieldName : Label;

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_disposalState.TryBegin()) {
            try {
                await Interop.ReleaseAsync(Element).ConfigureAwait(false);
            }
            finally {
                ((IDisposable)this).Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    ///     Receives blur/touched notifications from the browser module.
    /// </summary>
    /// <returns>A task representing the update.</returns>
    [JSInvokable]
    public async Task NotifyAutocompleteTouched() {
        await OnBlurAsync(new Microsoft.AspNetCore.Components.Web.FocusEventArgs());
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Receives the selected or typed value from the browser module.
    /// </summary>
    /// <param name="value">The current input value.</param>
    /// <param name="closeMenu">Whether the menu should be closed after the update.</param>
    /// <returns>A task representing the update.</returns>
    [JSInvokable]
    public async Task NotifyAutocompleteValueChanged(string? value, bool closeMenu) {
        if (!CanUpdateCurrentValue()) {
            return;
        }

        await UpdateCurrentValueAsync(value);
        await InvokeAsync(StateHasChanged);
    }

    internal void AddOptionChild(NTAutocompleteOption optionChild) {
        _optionChildren.Add(optionChild);
        RequestOptionChildrenRefresh();
    }

    internal void NotifyOptionChildChanged() {
        RequestOptionChildrenRefresh();
    }

    internal void RemoveOptionChild(NTAutocompleteOption optionChild) {
        _optionChildren.Remove(optionChild);
        RequestOptionChildrenRefresh();
    }

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-autocomplete");
        if (MenuItemAppearance == NTMenuItemAppearance.Condensed) {
            builder.Append(" nt-autocomplete-menu-items-condensed");
        }
    }

    /// <inheritdoc />
    protected override TrailingAdornmentState CreateTrailingAdornmentState(bool hasErrorText) {
        if (hasErrorText) {
            return base.CreateTrailingAdornmentState(hasErrorText);
        }

        return new TrailingAdornmentState {
            Icon = TrailingIcon ?? MaterialIcon.ArrowDropDown,
            Class = "nt-input-trailing nt-autocomplete-indicator",
            AriaHidden = "true"
        };
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(string? value) => value;

    /// <inheritdoc />
    [SuppressMessage("Reliability", "NTBA0003:Lifecycle method should use meaningful exception handling or owner ErrorBoundary coverage", Justification = "JS interop failures are handled by NTMenuFieldInterop.")]
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        var refreshAfterInitialOptionRegistration = !_hasRendered && _optionChildrenChangedBeforeFirstRender;
        _hasRendered = true;

        await Interop.AfterRenderAsync(JSRuntime, Element, firstRender);

        if (refreshAfterInitialOptionRegistration && !_disposalState.HasStarted) {
            _optionChildrenChangedBeforeFirstRender = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (!AllowCustomValue && !string.IsNullOrEmpty(value) && _optionChildren.Count > 0 && !_optionChildren.Any(option => !option.EffectiveDisabled && string.Equals(option.Value, value, StringComparison.Ordinal))) {
            result = null;
            validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field must match one of the available options.";
            return false;
        }

        result = value;
        validationErrorMessage = null;
        return true;
    }

    private static string FormatCustomValueOptionText(string format, string value) {
        var placeholderIndex = format.IndexOf("{0}", StringComparison.Ordinal);
        if (placeholderIndex < 0) {
            return format;
        }

        return string.Concat(format.AsSpan(0, placeholderIndex), value, format.AsSpan(placeholderIndex + 3));
    }

    private static string BuildCustomOptionJson(string customValueOptionFormat, bool fieldDisabled) {
        var builder = new StringBuilder();
        AppendOptionJson(builder, id: null, "nt-combobox-option", string.Empty, string.Empty, null, fieldDisabled, selected: false, isCustom: true, customValueOptionFormat, leadingIcon: null, group: null);
        return builder.ToString();
    }

    internal bool IsOptionSelected(NTAutocompleteOption option) => string.Equals(option.Value, CurrentValueAsString, StringComparison.Ordinal);

    internal static string BuildOptionJson(NTAutocompleteOption option, bool selected) {
        var builder = new StringBuilder();
        AppendOptionJson(builder, id: null, NTMenuOptionClass.ForSelectable(selected, option.EffectiveDisabled), option.Value, option.EffectiveLabel, option.SupportingText, option.EffectiveDisabled, selected, isCustom: false, customFormat: null, option.LeadingIcon, option.GroupLabel);
        return builder.ToString();
    }

    private static void AppendOptionJson(StringBuilder builder, string? id, string cssClass, string value, string label, string? supportingText, bool disabled, bool selected, bool isCustom, string? customFormat, NTIcon? leadingIcon, string? group) {
        builder.Append('{');
        if (id is not null) {
            AppendJsonProperty(builder, "id", id);
            builder.Append(',');
        }

        AppendJsonProperty(builder, "cssClass", cssClass);
        builder.Append(',');
        AppendJsonProperty(builder, "value", value);
        builder.Append(',');
        AppendJsonProperty(builder, "label", label);
        if (!string.IsNullOrWhiteSpace(supportingText)) {
            builder.Append(',');
            AppendJsonProperty(builder, "supportingText", supportingText);
        }

        if (!string.IsNullOrWhiteSpace(group)) {
            builder.Append(',');
            AppendJsonProperty(builder, "group", group);
        }

        builder.Append(',');
        AppendJsonProperty(builder, "disabled", disabled);
        builder.Append(',');
        AppendJsonProperty(builder, "selected", selected);
        builder.Append(',');
        AppendJsonProperty(builder, "isCustom", isCustom);
        if (customFormat is not null) {
            builder.Append(',');
            AppendJsonProperty(builder, "customFormat", customFormat);
        }

        if (leadingIcon is not null) {
            builder.Append(",\"leadingIcon\":{");
            AppendJsonProperty(builder, "icon", leadingIcon.Icon);
            builder.Append(',');
            AppendJsonProperty(builder, "cssClass", leadingIcon.ElementClass ?? "nt-components nt-icon material-symbols-outlined mi-medium");
            if (!string.IsNullOrWhiteSpace(leadingIcon.ElementStyle)) {
                builder.Append(',');
                AppendJsonProperty(builder, "style", leadingIcon.ElementStyle);
            }

            builder.Append(',');
            AppendJsonProperty(builder, "title", leadingIcon.ElementTitle ?? leadingIcon.Icon);
            builder.Append('}');
        }

        builder.Append('}');
    }

    private static void AppendJsonProperty(StringBuilder builder, string name, string value) {
        builder.Append('"');
        builder.Append(name);
        builder.Append("\":\"");
        builder.Append(JavaScriptEncoder.Default.Encode(value));
        builder.Append('"');
    }

    private static void AppendJsonProperty(StringBuilder builder, string name, bool value) {
        builder.Append('"');
        builder.Append(name);
        builder.Append("\":");
        builder.Append(value ? "true" : "false");
    }

    private bool CanUpdateCurrentValue() => !FieldReadOnly && !FieldDisabled;

    private void RequestOptionChildrenRefresh() {
        if (_hasRendered) {
            _ = InvokeAsync(StateHasChanged);
            return;
        }

        _optionChildrenChangedBeforeFirstRender = true;
    }
    private async Task OnChangeAsync(ChangeEventArgs args) {
        if (!CanUpdateCurrentValue()) {
            return;
        }

        await UpdateCurrentValueAsync(args.Value?.ToString());
    }

    private async Task UpdateCurrentValueAsync(string? value) {
        CurrentValueAsString = value;
        await BindAfter.InvokeAsync(CurrentValue);
    }
}

/// <summary>
///     Declares one suggestion inside an <see cref="NTAutocomplete" />.
/// </summary>
/// <remarks>
///     Place options directly in <see cref="NTAutocomplete.ChildContent" /> or inside an
///     <see cref="NTAutocompleteOptionGroup" />. The option renders as JSON metadata for static SSR and does not render a
///     visible menu item until the autocomplete browser module enhances the parent field.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders inert option metadata for the autocomplete enhancement module.",
    CompatibilityDetails = "Static SSR emits JSON metadata that the parent autocomplete browser module reads to build the enhanced suggestion menu. The option has no standalone interactive surface.")]
public sealed class NTAutocompleteOption : ComponentBase, IDisposable {
    private bool _hasParameterSnapshot;
    private bool _lastDisabled;
    private bool _lastGroupDisabled;
    private NTAutocompleteOptionGroup? _lastGroup;
    private NTIcon? _lastLeadingIcon;
    private string? _lastGroupLabel;
    private string? _lastLabel;
    private string? _lastSupportingText;
    private string? _lastValue;
    private string? _json;
    private bool _jsonSelected;

    /// <summary>
    ///     Gets or sets whether the suggestion is disabled in the enhanced menu.
    /// </summary>
    /// <remarks>
    ///     Disabled options are shown but cannot be selected. They are also excluded from strict value validation when
    ///     <see cref="NTAutocomplete.AllowCustomValue" /> is <see langword="false" />.
    /// </remarks>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets the visible suggestion label. When omitted, <see cref="Value" /> is used.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Gets or sets the optional leading icon.
    /// </summary>
    /// <remarks>
    ///     Use leading icons sparingly and only when the icon improves recognition. Selection state is rendered separately
    ///     and does not require callers to provide a check icon.
    /// </remarks>
    [Parameter]
    public NTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets optional supporting text shown below the label.
    /// </summary>
    [Parameter]
    public string? SupportingText { get; set; }

    /// <summary>
    ///     Gets or sets the value inserted into the text field and submitted with forms.
    /// </summary>
    /// <remarks>
    ///     Keep values stable and unique. An option is selected when this value exactly matches the parent autocomplete's bound value.
    ///     When <see cref="NTAutocomplete.AllowCustomValue" /> is <see langword="false" />, the typed value must match an enabled option value exactly.
    /// </remarks>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [CascadingParameter]
    private NTAutocomplete? Context { get; set; }

    [CascadingParameter]
    private NTAutocompleteOptionGroup? Group { get; set; }

    internal bool EffectiveDisabled => Disabled || Group?.Disabled == true;
    internal string EffectiveLabel => Label ?? Value;
    internal string? GroupLabel => Group?.Label;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTAutocompleteOption" /> class.
    /// </summary>
    public NTAutocompleteOption() { }


    /// <inheritdoc />
    public void Dispose() {
        Context?.RemoveOptionChild(this);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        builder.OpenElement(0, "script");
        builder.AddAttribute(1, "type", "application/json");
        builder.AddAttribute(2, "data-nt-autocomplete-option-definition", "true");
        var selected = Context!.IsOptionSelected(this);
        if (_json is null || _jsonSelected != selected || LeadingIcon is not null) {
            _json = NTAutocomplete.BuildOptionJson(this, selected);
            _jsonSelected = selected;
        }

        builder.AddContent(3, (MarkupString)_json);
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        if (Context is null) {
            throw new InvalidOperationException($"A {nameof(NTAutocompleteOption)} must be a child of {nameof(NTAutocomplete)}.");
        }

        Context.AddOptionChild(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (HasOptionParametersChanged()) {
            _json = null;
            Context?.NotifyOptionChildChanged();
        }
    }

    private bool HasOptionParametersChanged() {
        var groupDisabled = Group?.Disabled == true;
        var groupLabel = Group?.Label;
        var changed = _hasParameterSnapshot
            && (_lastDisabled != Disabled
                || _lastGroup != Group
                || _lastGroupDisabled != groupDisabled
                || _lastGroupLabel != groupLabel
                || _lastLabel != Label
                || !ReferenceEquals(_lastLeadingIcon, LeadingIcon)
                || _lastSupportingText != SupportingText
                || _lastValue != Value);

        _hasParameterSnapshot = true;
        _lastDisabled = Disabled;
        _lastGroup = Group;
        _lastGroupDisabled = groupDisabled;
        _lastGroupLabel = groupLabel;
        _lastLabel = Label;
        _lastLeadingIcon = LeadingIcon;
        _lastSupportingText = SupportingText;
        _lastValue = Value;
        return changed;
    }
}

/// <summary>
///     Groups related <see cref="NTAutocompleteOption" /> entries inside an <see cref="NTAutocomplete" />.
/// </summary>
/// <remarks>
///     Use groups for select-like section labels in longer suggestion lists. Add items through <see cref="ChildContent" />;
///     the group cascades its label and disabled state to child options without rendering a native <c>optgroup</c> element.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Groups autocomplete option metadata for the enhanced suggestion menu.",
    CompatibilityDetails = "Static SSR cascades group metadata to child options. The parent autocomplete browser module uses that metadata to render select-like grouped suggestions.")]
public sealed class NTAutocompleteOptionGroup : ComponentBase {
    /// <summary>
    ///     Gets or sets the grouped <see cref="NTAutocompleteOption" /> entries.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets whether all options in the group are disabled.
    /// </summary>
    /// <remarks>
    ///     Child options can still set their own <see cref="NTAutocompleteOption.Disabled" /> value, but a disabled group makes
    ///     every child option disabled in the enhanced menu and strict-value validation.
    /// </remarks>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets the group label rendered above the grouped suggestions.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        builder.OpenComponent<CascadingValue<NTAutocompleteOptionGroup>>(0);
        builder.AddAttribute(1, nameof(CascadingValue<NTAutocompleteOptionGroup>.Value), this);
        builder.AddAttribute(2, nameof(CascadingValue<NTAutocompleteOptionGroup>.IsFixed), false);
        builder.AddAttribute(3, nameof(CascadingValue<NTAutocompleteOptionGroup>.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}





