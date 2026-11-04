#pragma warning disable CS0618
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Interfaces;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Represents a searchable single-select input that filters options locally while preserving form binding semantics.
/// </summary>
/// <typeparam name="TInputType">The selected value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires interactive Blazor and page script for legacy searchable select behavior.",
    CompatibilityDetails = "The legacy searchable select can render a static value shell, but dropdown filtering, opening, and selection require interactive Blazor and its page script.")]
public partial class NTInputSelect<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInputType> : INTPageScriptComponent<NTInputSelect<TInputType>> {

    /// <summary>
    ///     The child options available for selection.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Optional sort selector applied to registered option children before filtering/rendering.
    /// </summary>
    [Parameter]
    public Func<NTInputSelectOption<TInputType>, IComparable?>? SortSelector { get; set; }

    /// <summary>
    ///     Allows values that are not represented by a registered option.
    /// </summary>
    [Parameter]
    public bool AllowFreeform { get; set; }

    /// <summary>
    ///     Direction used when <see cref="SortSelector" /> is supplied.
    /// </summary>
    [Parameter]
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// <inheritdoc />
    public override InputType Type => InputType.Select;

    /// <summary>
    ///     Gets the JS module path for the component.
    /// </summary>
    public string? JsModulePath => "./_content/NTComponents/Form/NTInputSelect.razor.js";

    /// <summary>
    ///     Gets the .NET reference used by JavaScript interop.
    /// </summary>
    private DotNetObjectReference<NTInputSelect<TInputType>>? DotNetObjectRef { get; set; }

    DotNetObjectReference<NTInputSelect<TInputType>>? INTPageScriptComponent<NTInputSelect<TInputType>>.DotNetObjectRef => DotNetObjectRef;

    /// <summary>
    ///     Gets the isolated JS module for this component.
    /// </summary>
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <summary>
    ///     Text shown when no options match the current search.
    /// </summary>
    [Parameter]
    public string NoResultsText { get; set; } = "No results found";

    /// <summary>
    ///     If true, opens the dropdown when the input gains focus.
    /// </summary>
    [Parameter]
    public bool OpenOnFocus { get; set; } = true;

    /// <summary>
    ///     Gets the JavaScript runtime for page-script interop.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <summary>
    ///     Gets the page script fragment that wires the component's isolated JavaScript module into SSR output.
    /// </summary>
    protected RenderFragment PageScript => builder => {
        builder.OpenComponent<NTPageScript>(0);
        builder.AddAttribute(1, nameof(NTPageScript.Src), JsModulePath);
        builder.CloseComponent();
    };

    private readonly List<SearchOption> _allOptions = [];
    private readonly NTDisposalState _disposalState = new();
    private readonly List<SearchOption> _filteredOptions = [];
    private readonly List<NTInputSelectOption<TInputType>> _optionChildren = [];
    private readonly EqualityComparer<TInputType?> _valueComparer = EqualityComparer<TInputType?>.Default;
    private ElementReference _searchInputElement;
    private int _focusedOptionIndex = -1;
    private bool _hasRendered;
    private bool _isOpen;
    private string? _lastSynchronizedText;
    private bool _hasRegisteredOptions;
    private bool _pendingBindAfter;
    private bool _optionsNeedRefresh;
    private string? _searchText;

    private IReadOnlyDictionary<string, object>? SearchInputAttributes => AdditionalAttributes?
        .Where(kvp => kvp.Key is not ("class" or "style" or "name" or "value" or "type"))
        .ToDictionary();

    private string HiddenValue => FormatValueAsString(CurrentValue) ?? string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTInputSelect{TInputType}" /> class.
    /// </summary>
    public NTInputSelect() => DotNetObjectRef = DotNetObjectReference.Create(this);

    /// <inheritdoc />
    protected override string? FormatValueAsString(TInputType? value) => BindConverter.FormatValue(value)?.ToString();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        RebuildOptions();
        SynchronizeSelectedText();
        RefreshFilteredOptions();
    }

    /// <inheritdoc />
    [SuppressMessage("Reliability", "NTBA0003:Lifecycle method should use meaningful exception handling or owner ErrorBoundary coverage", Justification = "Binding callbacks intentionally propagate to the consumer-owned ErrorBoundary.")]
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        _hasRendered = true;

        if (!DisposalStarted) {
            try {
                if (firstRender) {
                    var importedModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                    if (!DisposalStarted) {
                        IsolatedJsModule = importedModule;
                        if (TryGetInteropReferences(out var module, out var dotNetRef)) {
                            await module.InvokeVoidAsync("onLoad", Element, dotNetRef);
                        }
                    }
                    else {
                        await importedModule.DisposeAsync().ConfigureAwait(false);
                    }
                }

                if (TryGetInteropReferences(out var currentModule, out var currentDotNetRef)) {
                    await currentModule.InvokeVoidAsync("onUpdate", Element, currentDotNetRef);
                }

                if (!DisposalStarted) {
                    if (_optionsNeedRefresh) {
                        _optionsNeedRefresh = false;
                        RebuildOptions();
                        SynchronizeSelectedText();
                        RefreshFilteredOptions();
                        await InvokeAsync(StateHasChanged);
                    }
                    else if (_pendingBindAfter) {
                        _pendingBindAfter = false;
                        await BindAfter.InvokeAsync(CurrentValue);
                    }
                }
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during render.
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_disposalState.TryBegin()) {
            try {
                await DisposeAsyncInternal().ConfigureAwait(false);
            }
            finally {
                ((IDisposable)this).Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }

    internal void AddOptionChild(NTInputSelectOption<TInputType> optionChild) {
        _hasRegisteredOptions = true;
        if (!_optionChildren.Contains(optionChild)) {
            _optionChildren.Add(optionChild);
        }

        _optionsNeedRefresh = true;
        _ = InvokeAsync(StateHasChanged);
    }

    internal void RemoveOptionChild(NTInputSelectOption<TInputType> optionChild) {
        _optionChildren.Remove(optionChild);
        _optionsNeedRefresh = true;
        if (_hasRendered) {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    internal void NotifyOptionChildChanged(NTInputSelectOption<TInputType> optionChild) {
        if (_valueComparer.Equals(optionChild.Value, CurrentValue)) {
            var synchronizedText = optionChild.Label ?? string.Empty;
            if (_searchText is null || MatchesCurrentSelection(_searchText)) {
                _searchText = synchronizedText;
            }

            _lastSynchronizedText = synchronizedText;
        }

        if (!_hasRendered) {
            _optionsNeedRefresh = true;
            return;
        }

        _optionsNeedRefresh = true;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public override ValueTask SetFocusAsync() => _searchInputElement.FocusAsync();

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing && _disposalState.TryBegin()) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
        }

        base.Dispose(disposing);
    }

    private async ValueTask DisposeAsyncInternal() {
        if (IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
                await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }

            IsolatedJsModule = null;
        }

        if (DotNetObjectRef is IAsyncDisposable asyncDisposable) {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else {
            DotNetObjectRef?.Dispose();
        }

        DotNetObjectRef = null;
    }

    private bool DisposalStarted => _disposalState.HasStarted;

    private bool TryGetInteropReferences([NotNullWhen(true)] out IJSObjectReference? module, [NotNullWhen(true)] out DotNetObjectReference<NTInputSelect<TInputType>>? dotNetRef) {
        module = IsolatedJsModule;
        dotNetRef = DotNetObjectRef;
        return !DisposalStarted && module is not null && dotNetRef is not null;
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TInputType result, [NotNullWhen(false)] out string? validationErrorMessage) {
        try {
            if (typeof(TInputType) == typeof(bool)) {
                if (TryConvertToBool(value, out result)) {
                    validationErrorMessage = null;
                    return true;
                }
            }
            else if (typeof(TInputType) == typeof(bool?)) {
                if (TryConvertToNullableBool(value, out result)) {
                    validationErrorMessage = null;
                    return true;
                }
            }
            else if (BindConverter.TryConvertTo<TInputType>(value, CultureInfo.CurrentCulture, out var parsedValue)) {
                if (!AllowFreeform && !string.IsNullOrEmpty(value) && !HasMatchingOption(parsedValue)) {
                    result = default;
                    validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
                    return false;
                }

                result = parsedValue;
                validationErrorMessage = null;
                return true;
            }

            result = default;
            validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
            return false;
        }
        catch (InvalidOperationException ex) {
            throw new InvalidOperationException($"{GetType()} does not support the type '{typeof(TInputType)}'.", ex);
        }
    }

    private async Task OpenDropdownAsync(FocusEventArgs _) {
        if (FieldDisabled || FieldReadonly || !OpenOnFocus) {
            return;
        }

        _isOpen = true;
        RefreshFilteredOptions();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnInputChangedAsync(ChangeEventArgs args) {
        _searchText = args.Value?.ToString();
        _isOpen = true;

        if (!MatchesCurrentSelection(_searchText)) {
            if (AllowFreeform) {
                CurrentValueAsString = _searchText;
            }
            else {
                CurrentValue = default;
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await BindAfter.InvokeAsync(CurrentValue);
        }

        RefreshFilteredOptions();
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleKeyUpAsync(KeyboardEventArgs args) {
        if (_filteredOptions.Count == 0 && args.Key == "Escape") {
            _isOpen = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        switch (args.Key) {
            case "ArrowDown":
                if (!_isOpen) {
                    _isOpen = true;
                    RefreshFilteredOptions();
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                if (_filteredOptions.Count > 0) {
                    _focusedOptionIndex = _focusedOptionIndex < _filteredOptions.Count - 1 ? _focusedOptionIndex + 1 : 0;
                }
                break;

            case "ArrowUp":
                if (!_isOpen) {
                    _isOpen = true;
                    RefreshFilteredOptions();
                    _focusedOptionIndex = _filteredOptions.Count > 0 ? _filteredOptions.Count - 1 : -1;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                if (_filteredOptions.Count > 0) {
                    _focusedOptionIndex = _focusedOptionIndex > 0 ? _focusedOptionIndex - 1 : _filteredOptions.Count - 1;
                }
                break;

            case "Enter":
                if (_focusedOptionIndex >= 0 && _focusedOptionIndex < _filteredOptions.Count) {
                    await SelectOptionAsync(_filteredOptions[_focusedOptionIndex]);
                    return;
                }
                break;

            case "Escape":
                _isOpen = false;
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectOptionAsync(SearchOption option) {
        CurrentValue = option.Value;
        _searchText = option.Text;
        _lastSynchronizedText = option.Text;
        _isOpen = false;
        _focusedOptionIndex = -1;
        await BindAfter.InvokeAsync(CurrentValue);
        EditContext?.NotifyFieldChanged(FieldIdentifier);
        await InvokeAsync(StateHasChanged);
    }

    private bool HasMatchingOption(TInputType? value) => _allOptions.Any(option => _valueComparer.Equals(option.Value, value));

    private static bool IsDefaultValue(TInputType? value) => EqualityComparer<TInputType?>.Default.Equals(value, default);

    private bool MatchesCurrentSelection(string? searchText) => string.Equals(searchText, _lastSynchronizedText, StringComparison.Ordinal);

    private void RebuildOptions() {
        _allOptions.Clear();

        IEnumerable<NTInputSelectOption<TInputType>> options = _optionChildren;
        if (SortSelector is not null) {
            options = SortDirection == SortDirection.Descending
                ? options.OrderByDescending(SortSelector)
                : options.OrderBy(SortSelector);
        }

        var index = 0;
        foreach (var option in options) {
            _allOptions.Add(new SearchOption(option, index));
            index++;
        }
    }

    private void SynchronizeSelectedText() {
        var selectedOption = _allOptions.FirstOrDefault(option => _valueComparer.Equals(option.Value, CurrentValue));
        var synchronizedText = selectedOption?.Text;

        if (!AllowFreeform && selectedOption is null && _hasRegisteredOptions && !IsDefaultValue(CurrentValue)) {
            CurrentValue = default;
            synchronizedText = null;
            EditContext?.NotifyFieldChanged(FieldIdentifier);
            _pendingBindAfter = true;
        }

        if (_searchText is null || MatchesCurrentSelection(_searchText)) {
            _searchText = synchronizedText;
        }

        _lastSynchronizedText = synchronizedText;
    }

    private void RefreshFilteredOptions() {
        var searchText = _searchText?.Trim();
        var shouldFilter = !string.IsNullOrWhiteSpace(searchText) && !MatchesCurrentSelection(searchText);
        IEnumerable<SearchOption> options = _allOptions;

        if (shouldFilter) {
            var normalizedSearchText = searchText!;
            options = options
                .Where(option => option.Text.Contains(normalizedSearchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(option => option.Text.StartsWith(normalizedSearchText, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(option => option.Index);
        }

        _filteredOptions.Clear();
        _filteredOptions.AddRange(options);
        _focusedOptionIndex = _filteredOptions.Count > 0 ? Math.Clamp(_focusedOptionIndex, 0, _filteredOptions.Count - 1) : -1;
        if (_focusedOptionIndex < 0 && _filteredOptions.Count > 0) {
            _focusedOptionIndex = 0;
        }
    }

    /// <summary>
    ///     Invoked from JavaScript to close the dropdown when focus or pointer interaction moves outside the component.
    /// </summary>
    [JSInvokable]
    public async Task CloseDropdownFromJs() {
        if (!_isOpen) {
            return;
        }

        _isOpen = false;
        _focusedOptionIndex = -1;
        await InvokeAsync(StateHasChanged);
    }

    private static bool TryConvertToBool<TValue>(string? value, out TValue result) {
        if (bool.TryParse(value, out var @bool)) {
            result = (TValue)(object)@bool;
            return true;
        }

        result = default!;
        return false;
    }

    private static bool TryConvertToNullableBool<TValue>(string? value, out TValue result) {
        if (string.IsNullOrEmpty(value)) {
            result = default!;
            return true;
        }

        return TryConvertToBool(value, out result);
    }

    private sealed record SearchOption(NTInputSelectOption<TInputType> Option, int Index) {
        public RenderFragment? ChildContent => Option.ChildContent;
        public string Text => Option.Label ?? string.Empty;
        public TInputType? Value => Option.Value;
    }
}
