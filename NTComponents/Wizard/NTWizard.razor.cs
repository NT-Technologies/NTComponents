using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;
using NTComponents.Wizard;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Represents an NT-styled wizard component that manages multiple steps and provides navigation between them.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires Blazor interactivity for step navigation and submission workflow.",
    CompatibilityDetails = "The initial step can render statically, but next, previous, skip, validation, and submit callbacks depend on component state and an interactive render mode.")]
public partial class NTWizard : NTComponentBase, IDisposable {

    /// <summary>
    ///     The child content to be rendered inside the wizard.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the active step index.
    /// </summary>
    [Parameter]
    public int ActiveStepIndex { get; set; }

    /// <summary>
    ///     Callback invoked when the active step index changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> ActiveStepIndexChanged { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-wizard")
        .AddClass("nt-wizard-layout-horizontal", LayoutDirection == NTLayoutDirection.Horizontal)
        .AddClass("nt-wizard-vertical-on-small-screens", VerticalOnSmallScreens)
        .AddClass("nt-wizard-buttons-on-bottom", PushNavigationToBottom)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     The visual layout style of the wizard component.
    /// </summary>
    [Parameter]
    public NTLayoutDirection LayoutDirection { get; set; } = NTLayoutDirection.Vertical;

    /// <summary>
    ///     Determines whether the wizard should remain vertical when viewed on smaller screens.
    /// </summary>
    [Parameter]
    public bool VerticalOnSmallScreens { get; set; } = false;

    /// <summary>
    ///     A value indicating whether the "Next" button is disabled.
    /// </summary>
    [Parameter]
    public bool NextButtonDisabled { get; set; }

    /// <summary>
    ///     Callback invoked when the "Next" button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnNextButtonClicked { get; set; }

    /// <summary>
    ///     Callback invoked when the "Previous" button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnPreviousButtonClicked { get; set; }

    /// <summary>
    ///     The callback to be invoked when the wizard is submitted.
    /// </summary>
    [Parameter]
    public EventCallback OnSubmitCallback { get; set; }

    /// <summary>
    ///     A value indicating whether the "Previous" button is disabled.
    /// </summary>
    [Parameter]
    public bool PreviousButtonDisabled { get; set; }

    /// <summary>
    ///     A value indicating whether the "Submit" button is disabled.
    /// </summary>
    [Parameter]
    public bool SubmitButtonDisabled { get; set; }

    /// <summary>
    ///     Controls how Next/Submit buttons behave when the current form step is invalid.
    /// </summary>
    [Parameter]
    public NTWizardInvalidFormButtonBehavior InvalidFormButtonBehavior { get; set; } = NTWizardInvalidFormButtonBehavior.GrayOutOnly;

    /// <summary>
    ///     Gets or sets whether optional steps can be skipped.
    /// </summary>
    [Parameter]
    public bool AllowSkippingOptionalSteps { get; set; } = true;

    /// <summary>
    ///     Gets or sets how users can move between step indicators.
    /// </summary>
    [Parameter]
    public NTWizardNavigationMode NavigationMode { get; set; } = NTWizardNavigationMode.Linear;

    /// <summary>
    ///     Callback invoked when a step is skipped.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnStepSkipped { get; set; }

    /// <summary>
    ///     Callback invoked before navigation changes the active step. Return <c>false</c> to cancel navigation.
    /// </summary>
    [Parameter]
    public Func<NTWizardStepChangeContext, Task<bool>>? OnStepChanging { get; set; }

    /// <summary>
    ///     Callback invoked after navigation changes the active step.
    /// </summary>
    [Parameter]
    public EventCallback<NTWizardStepChangedEventArgs> OnStepChanged { get; set; }

    /// <summary>
    ///     The title of the wizard, displayed at the top.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    ///     If set, steps are rendered with display:none instead of being removed from the DOM.
    /// </summary>
    [Parameter]
    public bool HideStepsInsteadOfRemovingFromDom { get; set; }

    /// <summary>
    ///     Gets or sets whether Previous, Next, and Submit navigation buttons are pushed to the bottom of the component.
    ///     When false, the navigation buttons render directly below the current step content.
    /// </summary>
    [Parameter]
    public bool PushNavigationToBottom { get; set; } = true;

    private static int _childId;
    private readonly List<NTWizardStepBase> _steps = [];
    private readonly HashSet<int> _completedStepIds = [];
    private readonly HashSet<int> _invalidStepIds = [];
    private readonly HashSet<int> _skippedStepIds = [];
    private readonly HashSet<int> _visitedStepIds = [];
    private NTWizardStepBase? _currentStep => _steps.ElementAtOrDefault(_stepIndex);
    private int _stepIndex;
    private int _lastActiveStepIndexParameter;
    private bool _activeStepIndexParameterInitialized;
    private bool _currentStepFormIsValid = true;
    private EditContext? _currentStepEditContext;
    private bool CanSkipCurrentStep => AllowSkippingOptionalSteps && _currentStep?.Optional == true && HasNextEnterableStep;
    private bool HasNextEnterableStep => FindNextEnterableStepIndex(_stepIndex) is not null;
    private bool HasPreviousEnterableStep => FindPreviousEnterableStepIndex(_stepIndex) is not null;
    private bool IsCurrentStepFormInvalid => _currentStep is NTWizardFormStep && !_currentStepFormIsValid;
    private bool EffectiveNextButtonDisabled => NextButtonDisabled || !HasNextEnterableStep || ShouldDisableNavigationButtonForInvalidForm();
    private bool EffectiveSubmitButtonDisabled => SubmitButtonDisabled || ShouldDisableSubmitButtonForInvalidForm();
    private string? NavigationButtonInvalidClass => IsCurrentStepFormInvalid && ShouldValidateCurrentStepOnNavigation() && InvalidFormButtonBehavior == NTWizardInvalidFormButtonBehavior.GrayOutOnly ? "tnt-disabled" : null;
    private string? SubmitButtonInvalidClass => IsCurrentStepFormInvalid && InvalidFormButtonBehavior == NTWizardInvalidFormButtonBehavior.GrayOutOnly ? "tnt-disabled" : null;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        var activeStepIndexChanged = !_activeStepIndexParameterInitialized || ActiveStepIndex != _lastActiveStepIndexParameter;
        _lastActiveStepIndexParameter = ActiveStepIndex;
        _activeStepIndexParameterInitialized = true;

        if (_steps.Count == 0) {
            if (activeStepIndexChanged) {
                _stepIndex = Math.Max(0, ActiveStepIndex);
            }

            return;
        }

        if (!activeStepIndexChanged) {
            return;
        }

        var clampedIndex = Math.Clamp(ActiveStepIndex, 0, _steps.Count - 1);
        if (clampedIndex != _stepIndex && CanNavigateToStep(clampedIndex)) {
            MoveToStep(clampedIndex, notify: false);
        }
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) {
        if (SyncCurrentStepValidationSubscription()) {
            _ = InvokeAsync(StateHasChanged);
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    /// <inheritdoc />
    public void Dispose() {
        DetachCurrentStepValidationSubscription();
    }

    internal void AddChildStep(NTWizardStepBase step) {
        ArgumentNullException.ThrowIfNull(step);
        if (step._internalId == -1) {
            step._internalId = Interlocked.Increment(ref _childId);
        }

        if (!_steps.Any(s => s._internalId == step._internalId)) {
            _steps.Add(step);
        }

        MarkCurrentStepVisited();
        SyncCurrentStepValidationSubscription();
        StateHasChanged();
    }

    internal async Task NextStepAsync() {
        var nextStepIndex = FindNextEnterableStepIndex(_stepIndex);
        if (nextStepIndex is null) {
            return;
        }

        if (ShouldValidateCurrentStepOnNavigation() && !await ValidateCurrentStepAsync()) {
            StateHasChanged();
            return;
        }

        await OnNextButtonClicked.InvokeAsync(nextStepIndex.Value);
        await MoveToStepAsync(nextStepIndex.Value);
    }

    internal async Task PreviousStepAsync() {
        var previousStepIndex = FindPreviousEnterableStepIndex(_stepIndex);
        if (previousStepIndex is null) {
            return;
        }

        await OnPreviousButtonClicked.InvokeAsync(_stepIndex);
        await MoveToStepAsync(previousStepIndex.Value);
    }

    internal async Task NavigateToStepAsync(int stepIndex) {
        if (stepIndex == _stepIndex || !CanUseStepIndicator(stepIndex)) {
            return;
        }

        if (IsImmediateNextStep(stepIndex)) {
            await NextStepAsync();
            return;
        }

        if (stepIndex > _stepIndex && ShouldValidateCurrentStepOnNavigation() && !await ValidateCurrentStepAsync()) {
            StateHasChanged();
            return;
        }

        await MoveToStepAsync(stepIndex);
    }

    internal async Task SkipStepAsync() {
        if (!CanSkipCurrentStep || _currentStep is null) {
            return;
        }

        var skippedStepIndex = _stepIndex;
        var skippedStep = _currentStep;
        var nextStepIndex = FindNextEnterableStepIndex(_stepIndex);
        if (nextStepIndex is null) {
            return;
        }

        if (!await CanChangeStepAsync(skippedStepIndex, nextStepIndex.Value)) {
            return;
        }

        _skippedStepIds.Add(skippedStep._internalId);
        _completedStepIds.Remove(skippedStep._internalId);
        SetStepValidity(skippedStep, GetStepValidityWithoutMessageEmission(skippedStep));
        MoveToStep(nextStepIndex.Value, notify: true);
        await OnStepSkipped.InvokeAsync(skippedStepIndex);
    }

    internal void RemoveChildStep(NTWizardStepBase step) {
        ArgumentNullException.ThrowIfNull(step);
        _visitedStepIds.Remove(step._internalId);
        _completedStepIds.Remove(step._internalId);
        _invalidStepIds.Remove(step._internalId);
        _skippedStepIds.Remove(step._internalId);
        _steps.RemoveAll(s => s._internalId == step._internalId);
        if (_stepIndex >= _steps.Count) {
            _stepIndex = Math.Max(0, _steps.Count - 1);
        }

        MarkCurrentStepVisited();
        SyncCurrentStepValidationSubscription();
        StateHasChanged();
    }

    private string GetStepIndicatorClass(int stepIndex, bool canUse) => CssClassBuilder.Create("nt-wizard-step-indicator")
        .AddClass("current-step", _stepIndex == stepIndex)
        .AddClass("completed-step", IsStepCompleted(stepIndex))
        .AddClass("disabled-step", IsStepDisabled(stepIndex))
        .AddClass("available-step", canUse)
        .AddClass("invalid-step", IsStepInvalid(stepIndex))
        .AddClass("next-step", IsImmediateNextStep(stepIndex))
        .AddClass("optional-step", _steps.ElementAtOrDefault(stepIndex)?.Optional == true)
        .AddClass("skipped-step", IsStepSkipped(stepIndex))
        .Build();

    private static string GetStepStateValue(NTWizardStepState state) => state switch {
        NTWizardStepState.NotStarted => "notstarted",
        NTWizardStepState.Available => "available",
        NTWizardStepState.Current => "current",
        NTWizardStepState.Completed => "completed",
        NTWizardStepState.Skipped => "skipped",
        NTWizardStepState.Invalid => "invalid",
        _ => "disabled"
    };

    private bool CanUseStepIndicator(int stepIndex) => stepIndex == _stepIndex || (CanNavigateToStep(stepIndex) && (stepIndex <= _stepIndex || !ShouldDisableNavigationButtonForInvalidForm()));

    private string GetStepPanelId(int stepIndex) => $"{ElementId ?? "nt-wizard"}-step-panel-{stepIndex}";

    private string GetStepTabId(int stepIndex) => $"{ElementId ?? "nt-wizard"}-step-tab-{stepIndex}";

    private string GetStepIndicatorAriaLabel(int stepIndex, NTWizardStepState state) {
        var step = _steps.ElementAtOrDefault(stepIndex);
        if (step is null) {
            return $"Step {stepIndex + 1}";
        }

        return string.IsNullOrWhiteSpace(step.SubTitle)
            ? $"Step {stepIndex + 1}, {step.Title}, {state}"
            : $"Step {stepIndex + 1}, {step.Title}, {step.SubTitle}, {state}";
    }

    private async Task HandleKeyPressAsync(KeyboardEventArgs args) {
        if (args.Key != "Enter") {
            return;
        }

        if (!HasNextEnterableStep) {
            if (!EffectiveSubmitButtonDisabled) {
                await SubmitClickedAsync();
            }
        }
        else {
            if (!EffectiveNextButtonDisabled) {
                await NextStepAsync();
            }
        }
    }

    private async Task SubmitClickedAsync() {
        if (await ValidateCurrentStepAsync()) {
            await OnSubmitCallback.InvokeAsync();
        }
        else {
            StateHasChanged();
        }
    }

    private bool CanNavigateToStep(int stepIndex) {
        if (stepIndex < 0 || stepIndex >= _steps.Count || IsStepDisabled(stepIndex)) {
            return false;
        }

        if (NavigationMode == NTWizardNavigationMode.Free) {
            return true;
        }

        return IsStepVisited(stepIndex) || IsImmediateNextStep(stepIndex);
    }

    private int? FindNextEnterableStepIndex(int stepIndex) {
        for (var i = stepIndex + 1; i < _steps.Count; i++) {
            if (!IsStepDisabled(i)) {
                return i;
            }
        }

        return null;
    }

    private int? FindPreviousEnterableStepIndex(int stepIndex) {
        for (var i = stepIndex - 1; i >= 0; i--) {
            if (!IsStepDisabled(i)) {
                return i;
            }
        }

        return null;
    }

    private bool IsStepVisited(int stepIndex) {
        var step = _steps.ElementAtOrDefault(stepIndex);
        return step is not null && _visitedStepIds.Contains(step._internalId);
    }

    private bool IsImmediateNextStep(int stepIndex) => FindNextEnterableStepIndex(_stepIndex) == stepIndex;

    private bool IsStepCompleted(int stepIndex) {
        var step = _steps.ElementAtOrDefault(stepIndex);
        if (step is null || stepIndex == _stepIndex || IsStepSkipped(stepIndex) || IsStepInvalid(stepIndex) || IsStepDisabled(stepIndex)) {
            return false;
        }

        return step.Completed || _completedStepIds.Contains(step._internalId) || (IsStepVisited(stepIndex) && stepIndex < _stepIndex);
    }

    private bool IsStepDisabled(int stepIndex) => _steps.ElementAtOrDefault(stepIndex)?.Disabled == true;

    private bool IsStepInvalid(int stepIndex) {
        var step = _steps.ElementAtOrDefault(stepIndex);
        if (step is null) {
            return false;
        }

        return step.HasError || _invalidStepIds.Contains(step._internalId);
    }

    private bool IsStepSkipped(int stepIndex) {
        var step = _steps.ElementAtOrDefault(stepIndex);
        if (step is null) {
            return false;
        }

        return step.Skipped || _skippedStepIds.Contains(step._internalId);
    }

    private NTWizardStepState GetStepState(int stepIndex, bool canUse) {
        if (IsStepDisabled(stepIndex)) {
            return NTWizardStepState.Disabled;
        }

        if (_stepIndex == stepIndex) {
            return NTWizardStepState.Current;
        }

        if (IsStepInvalid(stepIndex)) {
            return NTWizardStepState.Invalid;
        }

        if (IsStepSkipped(stepIndex)) {
            return NTWizardStepState.Skipped;
        }

        if (IsStepCompleted(stepIndex)) {
            return NTWizardStepState.Completed;
        }

        if (canUse) {
            return NTWizardStepState.Available;
        }

        return NTWizardStepState.NotStarted;
    }

    private void MarkCurrentStepVisited() {
        if (_currentStep is not null) {
            _visitedStepIds.Add(_currentStep._internalId);
        }
    }

    private void MarkCurrentStepCompleted() {
        if (_currentStep is null || IsStepSkipped(_stepIndex)) {
            return;
        }

        _completedStepIds.Add(_currentStep._internalId);
        _skippedStepIds.Remove(_currentStep._internalId);
    }

    private async Task MoveToStepAsync(int stepIndex) {
        if (!await CanChangeStepAsync(_stepIndex, stepIndex)) {
            return;
        }

        MoveToStep(stepIndex, notify: true);
    }

    private void MoveToStep(int stepIndex, bool notify) {
        var previousStepIndex = _stepIndex;
        _stepIndex = stepIndex;
        MarkCurrentStepVisited();
        SyncCurrentStepValidationSubscription();
        if (notify) {
            _ = ActiveStepIndexChanged.InvokeAsync(stepIndex);
            _ = OnStepChanged.InvokeAsync(new NTWizardStepChangedEventArgs(previousStepIndex, stepIndex));
        }

        StateHasChanged();
    }

    private async Task<bool> CanChangeStepAsync(int fromStepIndex, int toStepIndex) {
        if (OnStepChanging is null) {
            return true;
        }

        return await OnStepChanging.Invoke(new NTWizardStepChangeContext(fromStepIndex, toStepIndex, _steps.ElementAtOrDefault(fromStepIndex), _steps.ElementAtOrDefault(toStepIndex)));
    }

    private bool ShouldValidateCurrentStepOnNavigation() => _currentStep is not NTWizardFormStep formStep || formStep.ValidateOnNavigation;

    private bool ShouldDisableNavigationButtonForInvalidForm() => IsCurrentStepFormInvalid && ShouldValidateCurrentStepOnNavigation() && InvalidFormButtonBehavior == NTWizardInvalidFormButtonBehavior.DisableButtons;

    private bool ShouldDisableSubmitButtonForInvalidForm() => IsCurrentStepFormInvalid && InvalidFormButtonBehavior == NTWizardInvalidFormButtonBehavior.DisableButtons;

    private bool SyncCurrentStepValidationSubscription() {
        var formStep = _currentStep as NTWizardFormStep;
        var editContext = formStep?.EditContext;
        var shouldRender = false;

        if (editContext is null) {
            DetachCurrentStepValidationSubscription();
            shouldRender = SetCurrentStepFormValidity(formStep is null);
            return shouldRender;
        }

        if (!ReferenceEquals(_currentStepEditContext, editContext)) {
            DetachCurrentStepValidationSubscription();
            _currentStepEditContext = editContext;
            _currentStepEditContext.OnFieldChanged += HandleCurrentStepFieldChanged;
            shouldRender = SetCurrentStepFormValidity(GetCurrentStepFormValidityWithoutMessageEmission()) || shouldRender;
        }

        return shouldRender;
    }

    private bool SetCurrentStepFormValidity(bool isValid) {
        var stepValidityChanged = _currentStep is NTWizardFormStep && SetStepValidity(_currentStep, isValid);
        if (_currentStepFormIsValid == isValid) {
            return stepValidityChanged;
        }

        _currentStepFormIsValid = isValid;
        return true;
    }

    private bool SetStepValidity(NTWizardStepBase step, bool isValid) {
        if (isValid) {
            return _invalidStepIds.Remove(step._internalId);
        }

        return _invalidStepIds.Add(step._internalId);
    }

    private void DetachCurrentStepValidationSubscription() {
        if (_currentStepEditContext is null) {
            return;
        }

        _currentStepEditContext.OnFieldChanged -= HandleCurrentStepFieldChanged;
        _currentStepEditContext = null;
    }

    private void HandleCurrentStepFieldChanged(object? sender, FieldChangedEventArgs args) {
        if (sender is not EditContext editContext || !ReferenceEquals(editContext, _currentStepEditContext)) {
            return;
        }

        if (SetCurrentStepFormValidity(GetCurrentStepFormValidityAfterFieldChange(editContext))) {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private async Task<bool> ValidateCurrentStepAsync() {
        if (_currentStep is null) {
            return true;
        }

        var isValid = _currentStep is NTWizardFormStep formStep
            ? await formStep.FormValidAsync()
            : await _currentStep.ValidateStepAsync();
        SetStepValidity(_currentStep, isValid);
        SetCurrentStepFormValidity(isValid);
        if (isValid) {
            MarkCurrentStepCompleted();
        }

        return isValid;
    }

    private bool GetCurrentStepFormValidityWithoutMessageEmission() {
        if (_currentStep is not NTWizardFormStep formStep) {
            return true;
        }

        return formStep.RequiredFieldsSatisfiedWithoutMessageEmission();
    }

    private static bool GetStepValidityWithoutMessageEmission(NTWizardStepBase step) => step is not NTWizardFormStep formStep || formStep.RequiredFieldsSatisfiedWithoutMessageEmission();

    private bool GetCurrentStepFormValidityAfterFieldChange(EditContext editContext) {
        if (_currentStep is not NTWizardFormStep { ValidateOnInput: true }) {
            return GetCurrentStepFormValidityWithoutMessageEmission();
        }

        return GetCurrentStepFormValidityWithoutMessageEmission() && !editContext.GetValidationMessages().Any();
    }
}

/// <summary>
///     Defines how wizard progression buttons should behave when the current form step is invalid.
/// </summary>
public enum NTWizardInvalidFormButtonBehavior {

    /// <summary>
    ///     Applies disabled styling but keeps buttons clickable so validation feedback can be shown on click.
    /// </summary>
    GrayOutOnly,

    /// <summary>
    ///     Fully disables buttons while the current form step is invalid.
    /// </summary>
    DisableButtons
}

/// <summary>
///     Defines how users can navigate between wizard step indicators.
/// </summary>
public enum NTWizardNavigationMode {

    /// <summary>
    ///     Users may move to the immediate next enabled step or previously visited steps.
    /// </summary>
    Linear,

    /// <summary>
    ///     Users may move to any enabled step.
    /// </summary>
    Free
}

/// <summary>
///     Defines the computed state of a wizard step.
/// </summary>
public enum NTWizardStepState {

    /// <summary>
    ///     The step has not been reached yet.
    /// </summary>
    NotStarted,

    /// <summary>
    ///     The step can be entered.
    /// </summary>
    Available,

    /// <summary>
    ///     The step is currently active.
    /// </summary>
    Current,

    /// <summary>
    ///     The step has been completed.
    /// </summary>
    Completed,

    /// <summary>
    ///     The step has been skipped.
    /// </summary>
    Skipped,

    /// <summary>
    ///     The step is invalid.
    /// </summary>
    Invalid,

    /// <summary>
    ///     The step is disabled.
    /// </summary>
    Disabled
}

/// <summary>
///     Provides context for a wizard step navigation request.
/// </summary>
public sealed record NTWizardStepChangeContext(int FromStepIndex, int ToStepIndex, NTWizardStepBase? FromStep, NTWizardStepBase? ToStep);

/// <summary>
///     Provides data for a completed wizard step navigation change.
/// </summary>
public sealed record NTWizardStepChangedEventArgs(int PreviousStepIndex, int CurrentStepIndex);
