using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NTComponents.Wizard;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Defines the contract for an NT wizard form step.
/// </summary>
public interface INTWizardFormStep {

    /// <summary>
    ///     The child content of the form step, which is a render fragment that takes an <see cref="EditContext" />.
    /// </summary>
    RenderFragment<EditContext> ChildContent { get; set; }

    /// <summary>
    ///     Sets the form fields to be disabled.
    /// </summary>
    bool Disabled { get; set; }

    /// <summary>
    ///     The visual appearance used by descendant NT input components.
    /// </summary>
    NTFormAppearance Appearance { get; set; }

    /// <summary>
    ///     The density used by descendant NT input components.
    /// </summary>
    NTFormDensity Density { get; set; }

    /// <summary>
    ///     The name of the form. This is optional.
    /// </summary>
    string? FormName { get; set; }

    /// <summary>
    ///     A value indicating whether to include a <see cref="DataAnnotationsValidator" /> in the form. Defaults to <c>true</c>.
    /// </summary>
    bool IncludeDataAnnotationsValidator { get; set; }

    /// <summary>
    ///     The model object used for data binding in the form.
    /// </summary>
    object Model { get; set; }

    /// <summary>
    ///     The callback to invoke when the form submission is invalid.
    /// </summary>
    EventCallback<object> OnInvalidSubmitCallback { get; set; }

    /// <summary>
    ///     The callback to invoke when the form submission is valid.
    /// </summary>
    EventCallback<object> OnValidSubmitCallback { get; set; }

    /// <summary>
    ///     Sets the form fields to be read-only.
    /// </summary>
    bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether required inputs should show required supporting text.
    /// </summary>
    bool ShowRequiredSupportingText { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the form should validate as input changes.
    /// </summary>
    bool ValidateOnInput { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the form should validate before navigating to the next step.
    /// </summary>
    bool ValidateOnNavigation { get; set; }

    /// <summary>
    ///     Gets or sets the supporting text shown for required inputs.
    /// </summary>
    string RequiredSupportingText { get; set; }

    /// <summary>
    ///     Validates the form and invokes the appropriate callback based on the validation result.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}" /> that resolves to <c>true</c> if the form is valid; otherwise <c>false</c>.</returns>
    Task<bool> FormValidAsync();
}

/// <summary>
///     Represents a wizard step that contains an <see cref="NTForm" />.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The step participates in NTWizard navigation and form submit callbacks, which require an interactive render mode for validation and step transitions.")]
public class NTWizardFormStep : NTWizardStepBase, INTWizardFormStep {

    /// <inheritdoc />
    [Parameter, EditorRequired]
    public RenderFragment<EditContext> ChildContent { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public NTFormAppearance Appearance { get; set; } = NTFormAppearance.Outlined;

    /// <inheritdoc />
    [Parameter]
    public NTFormDensity Density { get; set; } = NTFormDensity.Standard;

    /// <inheritdoc />
    [Parameter]
    public string? FormName { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool IncludeDataAnnotationsValidator { get; set; } = true;

    /// <inheritdoc />
    [Parameter]
    public object Model { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public EventCallback<object> OnInvalidSubmitCallback { get; set; }

    /// <inheritdoc />
    [Parameter]
    public EventCallback<object> OnValidSubmitCallback { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool ShowRequiredSupportingText { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool ValidateOnInput { get; set; } = true;

    /// <inheritdoc />
    [Parameter]
    public bool ValidateOnNavigation { get; set; } = true;

    /// <inheritdoc />
    [Parameter]
    public string RequiredSupportingText { get; set; } = "Required";

    private NTForm _form = default!;

    internal EditContext? EditContext => _form?.EditContext;

    /// <summary>
    ///     Performs a trim-safe required-field check without touching form edit context validation messages.
    /// </summary>
    /// <returns><c>true</c> when all required model fields contain values; otherwise <c>false</c>.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Used only for UI affordance in wizard button state; runtime model metadata is available in component usage.")]
    internal bool RequiredFieldsSatisfiedWithoutMessageEmission() {
        var model = _form?.Model ?? Model;
        if (model is null) {
            return false;
        }

        foreach (var property in _requiredProperties.GetOrAdd(model.GetType(), FindRequiredProperties)) {
            var value = property.GetValue(model);
            if (value is null) {
                return false;
            }

            if (value is string str && string.IsNullOrWhiteSpace(str)) {
                return false;
            }
        }

        return true;
    }

    // The required-property set depends only on the model type, so it is computed once per type instead of on every field change.
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _requiredProperties = new();

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Used only for UI affordance in wizard button state; runtime model metadata is available in component usage.")]
    private static PropertyInfo[] FindRequiredProperties(Type modelType) => [.. modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0 && property.CanRead && property.GetCustomAttributes<RequiredAttribute>(inherit: true).Any())];

    /// <inheritdoc />
    public async Task<bool> FormValidAsync() {
#if NET11_0_OR_GREATER
        var isValid = _form?.EditContext is { } editContext && await editContext.ValidateAsync();
#else
        var isValid = _form?.EditContext?.Validate() == true;
#endif
        if (isValid) {
            await OnValidSubmitCallback.InvokeAsync(Model);
            return true;
        }

        await OnInvalidSubmitCallback.InvokeAsync(Model);
        return false;
    }

    /// <inheritdoc />
    public override RenderFragment Render() => new(builder => {
        builder.OpenComponent<NTForm>(0);
        builder.AddComponentParameter(10, nameof(NTForm.Model), Model);
        builder.AddComponentParameter(15, nameof(NTForm.Appearance), Appearance);
        builder.AddComponentParameter(16, nameof(NTForm.Density), Density);
        builder.AddComponentParameter(17, nameof(NTForm.Disabled), Disabled);
        builder.AddComponentParameter(18, nameof(NTForm.ReadOnly), ReadOnly);
        builder.AddComponentParameter(19, nameof(NTForm.ShowRequiredSupportingText), ShowRequiredSupportingText);
        builder.AddComponentParameter(20, nameof(NTForm.RequiredSupportingText), RequiredSupportingText);
        builder.AddComponentParameter(21, nameof(NTForm.BindOnInput), ValidateOnInput);
        builder.AddAttribute(25, "class", "nt-wizard-form");
        if (!string.IsNullOrWhiteSpace(FormName)) {
            builder.AddComponentParameter(30, nameof(NTForm.FormName), FormName);
        }

        builder.SetKey(_internalId);
        builder.AddComponentParameter(60, nameof(NTForm.ChildContent), new RenderFragment<EditContext>(editContext => new RenderFragment(b => {
            if (IncludeDataAnnotationsValidator) {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
            }

            b.AddContent(10, ChildContent(editContext));
        })));
        builder.AddComponentReferenceCapture(70, component => {
            if (component is NTForm form) {
                _form = form;
            }
        });
        builder.CloseComponent();
    });

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Model, nameof(Model));
    }
}
