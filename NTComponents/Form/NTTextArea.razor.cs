using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Ext;
using NTComponents.Interfaces;
using NTComponents.Core;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned multiline text field.
/// </summary>
/// <remarks>
///     Use <see cref="NTTextArea" /> for free-form multiline text such as notes, descriptions, comments, and messages.
///     Prefer <see cref="NTInputText" /> for short single-line values, search, email, telephone, URL, and password entry.
/// </remarks>
/// <remarks>
///     Keep labels concise and use supporting text for limits, formatting expectations, or privacy context. Add native
///     <c>maxlength</c> with the built-in character counter for bounded fields. Use <see cref="MinVisibleLines" /> for
///     the initial native <c>rows</c> value, and pass native <c>cols</c>, <c>wrap</c>, and <c>spellcheck</c> attributes
///     through <c>AdditionalAttributes</c> when the browser behavior should be explicit.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a native textarea and enhances autosizing with JavaScript.",
    CompatibilityDetails = "Static SSR emits the textarea, labels, supporting text, and named value. Autosize behavior, live binding, and validation updates require browser or Blazor enhancement.")]
public partial class NTTextArea : INTPageScriptComponent<NTTextArea> {
    private const string TextAreaJsModulePath = "./_content/NTComponents/Form/NTTextArea.razor.js";
    private readonly NTDisposalState _disposalState = new();
    private IReadOnlyDictionary<string, object?>? _additionalInputAttributes;
    private (int MinLines, int? MaxLines, bool AutoGrow, bool SizeByContent, string? Style) _additionalInputAttributesKey;

    private static readonly HashSet<string> TextAreaExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "name",
        "title",
        "autofocus",
        "autocomplete",
        "readonly",
        "disabled",
        "required",
        "min",
        "max",
        "type",
        "placeholder",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage",
        "oninput",
        "value"
    };

    /// <summary>
    ///     Gets the .NET object reference used by JavaScript interop after an interactive renderer is attached.
    /// </summary>
    private DotNetObjectReference<NTTextArea>? DotNetObjectRef { get; set; }

    DotNetObjectReference<NTTextArea>? INTPageScriptComponent<NTTextArea>.DotNetObjectRef => DotNetObjectRef;

    /// <summary>
    ///     Gets the isolated JavaScript module used by the interactive renderer.
    /// </summary>
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <inheritdoc />
    public string? JsModulePath => TextAreaJsModulePath;

    string? INTComponentBase.ElementClass => GetRootClass();

    string? INTComponentBase.ElementStyle => ElementStyle;

    /// <summary>
    ///     Gets the JavaScript runtime for interactive module loading.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTTextArea" /> class.
    /// </summary>
    public NTTextArea() => DotNetObjectRef = DotNetObjectReference.Create(this);

    /// <summary>
    ///     Gets or sets a value indicating whether the textarea should grow vertically as the user types.
    /// </summary>
    /// <remarks>
    ///     This is enabled by default so multiline text remains visible without forcing users into an internal scrollbar.
    ///     Set <see cref="MaxVisibleLines" /> when the field should stop growing after a known number of visible lines.
    /// </remarks>
    [Parameter]
    public bool AutoGrow { get; set; } = true;

    /// <summary>
    ///     Gets or sets the minimum number of visible text lines.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>2</c>, which is also the initial rendered row count. Autogrow textareas will not shrink below
    ///     this number of visible lines.
    /// </remarks>
    [Parameter]
    public int MinVisibleLines { get; set; } = 2;

    /// <summary>
    ///     Gets or sets the maximum number of visible text lines before the textarea starts scrolling.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>5</c> so autogrow textareas expand for short content, then use internal scrolling for longer
    ///     content. Set to <see langword="null" /> only when unbounded vertical growth is intended.
    /// </remarks>
    [Parameter]
    public int? MaxVisibleLines { get; set; } = 5;

    /// <summary>
    ///     Gets or sets a value indicating whether the browser should size the textarea to its content when supported.
    /// </summary>
    /// <remarks>
    ///     Use this for compact comments or notes where content-driven height is useful. For long-form fields, prefer a
    ///     stable row count so surrounding layouts do not shift while users type.
    /// </remarks>
    [Parameter]
    public bool SizeByContent { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => TextAreaExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-textarea";

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalInputAttributes() {
        // The attributes depend only on these inputs, so rebuild them only when one changes.
        var key = (MinVisibleLines, MaxVisibleLines, ShouldAutoGrow, SizeByContent, GetAdditionalAttributeString("style"));
        if (_additionalInputAttributes is not null && key == _additionalInputAttributesKey) {
            return _additionalInputAttributes;
        }

        var attributes = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        attributes["rows"] = MinVisibleLines.ToString(CultureInfo.InvariantCulture);

        if (ShouldAutoGrow) {
            attributes["data-nt-textarea-autogrow"] = "true";
            attributes["data-nt-textarea-min-visible-lines"] = MinVisibleLines.ToString(CultureInfo.InvariantCulture);

            if (MaxVisibleLines is int maxVisibleLines) {
                attributes["data-nt-textarea-max-visible-lines"] = maxVisibleLines.ToString(CultureInfo.InvariantCulture);
            }
        }

        var style = BuildTextAreaStyle(key.Item5);
        if (style is not null) {
            attributes["style"] = style;
        }

        _additionalInputAttributesKey = key;
        return _additionalInputAttributes = attributes;
    }

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-textarea");
        if (ShouldAutoGrow) {
            builder.Append(" nt-textarea-autogrow");
        }

        if (SizeByContent) {
            builder.Append(" nt-textarea-size-by-content");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (MinVisibleLines < 1) {
            throw new InvalidOperationException($"{nameof(MinVisibleLines)} must be a positive number.");
        }

        if (MaxVisibleLines is < 1) {
            throw new InvalidOperationException($"{nameof(MaxVisibleLines)} must be a positive number when specified.");
        }

        if (MaxVisibleLines is int maxVisibleLines && maxVisibleLines < MinVisibleLines) {
            throw new InvalidOperationException($"{nameof(MaxVisibleLines)} must be greater than or equal to {nameof(MinVisibleLines)}.");
        }

        base.OnParametersSet();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_disposalState.TryBegin()) {
            try {
                await DisposeJsModuleAsync();
                DotNetObjectRef?.Dispose();
                DotNetObjectRef = null;
            }
            finally {
                ((IDisposable)this).Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        if (RendererInfo.IsInteractive && !DisposalStarted) {
            try {
                if (ShouldAutoGrow) {
                    if (IsolatedJsModule is null) {
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
                }
                else {
                    await DisposeJsModuleAsync();
                }
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during render.
            }
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing && _disposalState.TryBegin()) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
        }

        base.Dispose(disposing);
    }

    private bool ShouldAutoGrow => AutoGrow && !SizeByContent;

    private bool DisposalStarted => _disposalState.HasStarted;

    private async ValueTask DisposeJsModuleAsync() {
        if (IsolatedJsModule is null) {
            return;
        }

        try {
            await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
            await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during disposal.
        }

        IsolatedJsModule = null;
    }

    private bool TryGetInteropReferences([NotNullWhen(true)] out IJSObjectReference? module, [NotNullWhen(true)] out DotNetObjectReference<NTTextArea>? dotNetRef) {
        module = IsolatedJsModule;
        dotNetRef = DotNetObjectRef;
        return !DisposalStarted && module is not null && dotNetRef is not null;
    }

    private string? BuildTextAreaStyle(string? style) {
        if (!SizeByContent) {
            return style;
        }

        const string FieldSizingStyle = "field-sizing:content;";
        return string.IsNullOrWhiteSpace(style)
            ? FieldSizingStyle
            : $"{style.TrimEnd().TrimEnd(';')};{FieldSizingStyle}";
    }
}
