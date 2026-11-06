using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
/// Rich text editor shell. Rendering, formatting, SSR enhancement, and tool behavior live in TypeScript; this class
/// only supplies parameters, markup metadata, and optional interactive callbacks.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires browser and Blazor interactivity for rich text editing.",
    CompatibilityDetails = "The editor depends on JavaScript interop, contenteditable behavior, and JSInvokable callbacks for its primary editing contract. Static SSR can only emit the initial shell.")]
public partial class NTRichTextEditor : INTPageScriptComponent<NTRichTextEditor> {
    private static readonly HashSet<string> RichTextEditorExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "aria-describedby",
        "aria-errormessage",
        "aria-invalid",
        "aria-label",
        "aria-labelledby",
        "autofocus",
        "class",
        "contenteditable",
        "data-bind-on-input",
        "data-editable",
        "data-empty",
        "data-interactive",
        "data-maxlength",
        "data-placeholder",
        "data-received-value-count",
        "id",
        "lang",
        "maxlength",
        "minlength",
        "name",
        "onkeydown",
        "placeholder",
        "readonly",
        "required",
        "role",
        "spellcheck",
        "style",
        "tabindex",
        "title",
        "type",
        "value"
    };

    private const string ScriptLoadErrorText = "The editor could not load. Reload the page to edit this field.";

    private static readonly ConcurrentDictionary<string, string> _ariaKeyShortcuts = new(StringComparer.Ordinal);

    private readonly IJSRuntime _jsRuntime;
    private readonly NTDisposalState _disposalState = new();
    private ElementReference _editorElement;
    private long _receivedValueCount;
    private long _valueSequence;
    private long _markupSequence;
    private object? _boundModel;
    private bool _scriptLoadFailed;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// Gets the .NET object reference used for JavaScript interop.
    /// </summary>
    private DotNetObjectReference<NTRichTextEditor>? DotNetObjectRef { get; set; }

    DotNetObjectReference<NTRichTextEditor>? INTPageScriptComponent<NTRichTextEditor>.DotNetObjectRef => DotNetObjectRef;

    /// <summary>
    /// Gets the isolated JavaScript module used by the component.
    /// </summary>
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <inheritdoc />
    public string? JsModulePath => "./_content/NTComponents/Editors/NTRichTextEditor.razor.js";

    string? INTComponentBase.ElementClass => GetRootClass();

    string? INTComponentBase.ElementStyle => ElementStyle;

    /// <summary>
    /// Gets the editor input type for compatibility with earlier rich editor callers.
    /// </summary>
    public InputType Type => InputType.TextArea;

    /// <summary>
    /// Gets or sets an explicit error message for compatibility with the legacy form field API.
    /// Prefer <see cref="NTFormControlBaseCore{TValue}.ErrorText" /> for new code.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the legacy end icon alias. Prefer <see cref="NTFormControlBase{TValue}.TrailingIcon" /> for new code.
    /// </summary>
    [Parameter]
    public NTIcon? EndIcon { get; set; }

    /// <summary>
    /// Gets or sets the legacy character-counter alias. Prefer <see cref="NTInputBase{TValue}.ShowCharacterCounter" /> for new code.
    /// </summary>
    [Parameter]
    public bool? ShowInputLength { get; set; }

    /// <summary>
    /// Gets or sets the legacy start icon alias. Prefer <see cref="NTFormControlBase{TValue}.LeadingIcon" /> for new code.
    /// </summary>
    [Parameter]
    public NTIcon? StartIcon { get; set; }

    /// <summary>
    /// Gets or sets tooltip content for compatibility with the legacy form field API.
    /// </summary>
    [Parameter]
    public RenderFragment? Tooltip { get; set; }

    /// <summary>
    /// Gets or sets the tooltip icon for compatibility with the legacy form field API.
    /// </summary>
    [Parameter]
    public NTIcon TooltipIcon { get; set; } = MaterialIcon.Help;

    private MarkupString _markupValue;

    /// <summary>
    /// Gets the current editor HTML markup produced by the TypeScript editor surface.
    /// </summary>
    /// <remarks>
    /// This markup, like the bound value, is sanitized in the browser and received from it as-is: the server does not
    /// sanitize it. Sanitize it on the server before storing it or rendering it to other users.
    /// </remarks>
    public MarkupString MarkupValue => _markupValue;

    /// <summary>
    /// Invoked when <see cref="MarkupValue" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<MarkupString> MarkupValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the toolbar items rendered by the editor.
    /// </summary>
    /// <remarks>
    /// When not provided, <see cref="DefaultToolbarButtons" /> is used.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<INTRichTextEditorButton>? ToolbarButtons { get; set; }

    /// <summary>
    /// Gets or sets the tool plugins available to the editor.
    /// </summary>
    /// <remarks>
    /// When not provided, <see cref="DefaultTools" /> is used.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<INTRichTextEditorTool>? Tools { get; set; }

    /// <summary>
    /// Gets the default toolbar items used by the editor.
    /// </summary>
    public static readonly IReadOnlyList<INTRichTextEditorButton> DefaultToolbarButtons = [
            new NTRichTextEditorButton("undo", "Undo", "Undo", icon: MaterialIcon.Undo, shortcut: "Ctrl+Z"),
            new NTRichTextEditorButton("redo", "Redo", "Redo", icon: MaterialIcon.Redo, shortcut: "Ctrl+Y"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("paragraph", "Paragraph", "Paragraph", icon: MaterialIcon.Subject, shortcut: "Ctrl+Alt+0"),
            new NTRichTextEditorButton("heading", "Heading 1", "Heading 1", icon: MaterialIcon.LooksOne, value: "1", shortcut: "Ctrl+Alt+1"),
            new NTRichTextEditorButton("heading", "Heading 2", "Heading 2", icon: MaterialIcon.LooksTwo, value: "2", shortcut: "Ctrl+Alt+2"),
            new NTRichTextEditorButton("heading", "Heading 3", "Heading 3", icon: MaterialIcon.Looks3, value: "3", shortcut: "Ctrl+Alt+3"),
            new NTRichTextEditorButton("heading", "Heading 4", "Heading 4", icon: MaterialIcon.Looks4, value: "4", shortcut: "Ctrl+Alt+4"),
            new NTRichTextEditorButton("heading", "Heading 5", "Heading 5", icon: MaterialIcon.Looks5, value: "5", shortcut: "Ctrl+Alt+5"),
            new NTRichTextEditorButton("heading", "Heading 6", "Heading 6", icon: MaterialIcon.Looks6, value: "6", shortcut: "Ctrl+Alt+6"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("alignLeft", "Align Left", "Align left", icon: MaterialIcon.FormatAlignLeft, shortcut: "Ctrl+Shift+L"),
            new NTRichTextEditorButton("alignCenter", "Align Center", "Align center", icon: MaterialIcon.FormatAlignCenter, shortcut: "Ctrl+Shift+E"),
            new NTRichTextEditorButton("alignRight", "Align Right", "Align right", icon: MaterialIcon.FormatAlignRight, shortcut: "Ctrl+Shift+R"),
            new NTRichTextEditorButton("alignJustify", "Justify", "Justify", icon: MaterialIcon.FormatAlignJustify, shortcut: "Ctrl+Shift+J"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("unorderedList", "Bulleted List", "Bulleted list", icon: MaterialIcon.FormatListBulleted, shortcut: "Ctrl+Alt+7"),
            new NTRichTextEditorButton("orderedList", "Numbered List", "Numbered list", icon: MaterialIcon.FormatListNumbered, shortcut: "Ctrl+Alt+8"),
            new NTRichTextEditorButton("blockquote", "Quote Block", "Quote block", icon: MaterialIcon.FormatQuote, shortcut: "Ctrl+Alt+Q"),
            new NTRichTextEditorButton("codeBlock", "Code Block", "Code block", icon: MaterialIcon.Code, shortcut: "Ctrl+Alt+C"),
            new NTRichTextEditorButton("table", "Insert Table", "Insert table", icon: MaterialIcon.TableChart, shortcut: "Ctrl+Alt+T"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("bold", "Bold", "Bold", icon: MaterialIcon.FormatBold, shortcut: "Ctrl+B"),
            new NTRichTextEditorButton("italic", "Italic", "Italic", icon: MaterialIcon.FormatItalic, shortcut: "Ctrl+I"),
            new NTRichTextEditorButton("underline", "Underline", "Underline", icon: MaterialIcon.FormatUnderlined, shortcut: "Ctrl+U"),
            new NTRichTextEditorButton("strikeThrough", "Strikethrough", "Strikethrough", icon: MaterialIcon.StrikethroughS, shortcut: "Ctrl+Shift+S"),
            new NTRichTextEditorButton("textColor", "Text Color", "Text color", icon: MaterialIcon.FormatColorText, shortcut: "Ctrl+Alt+X"),
            new NTRichTextEditorButton("link", "Insert Link", "Insert link", icon: MaterialIcon.InsertLink, shortcut: "Ctrl+K"),
            new NTRichTextEditorButton("ariaLabel", "Accessible Label", "Add accessible label", icon: MaterialIcon.Label),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("image", "Insert Image", "Insert image", icon: MaterialIcon.Image, shortcut: "Ctrl+Alt+M"),
            new NTRichTextEditorButton("iframe", "Insert Iframe", "Insert iframe", icon: MaterialIcon.Web, shortcut: "Ctrl+Alt+F"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("editHtml", "Edit HTML", "Edit HTML source", icon: MaterialIcon.Html)
        ];

    /// <summary>
    /// Gets the default tool plugins used by the editor.
    /// </summary>
    public static readonly IReadOnlyList<INTRichTextEditorTool> DefaultTools = [
            new NTRichTextEditorTool("image", CreateToolPanelTemplate<EditorToolImagePanel>()),
            new NTRichTextEditorTool("table", CreateToolPanelTemplate<EditorToolTablePanel>()),
            new NTRichTextEditorTool("textColor", CreateToolPanelTemplate<EditorToolTextColorPanel>()),
            new NTRichTextEditorTool("link", CreateToolPanelTemplate<EditorToolLinkPanel>()),
            new NTRichTextEditorTool("ariaLabel", CreateToolPanelTemplate<EditorToolAriaLabelPanel>()),
            new NTRichTextEditorTool("iframe", CreateToolPanelTemplate<EditorToolIframePanel>())
        ];

    /// <summary>
    /// Gets the page script fragment that ensures the editor module is available during SSR rendering.
    /// </summary>
    protected RenderFragment PageScript { get; private set; } = _ => { };

    private IReadOnlyList<INTRichTextEditorTool> _activeToolsToRender = Array.Empty<INTRichTextEditorTool>();

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => RichTextEditorExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override bool UsesSeparateFormPostValue => true;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-rich-text-editor";

    private int? MaxLength => TryParseAdditionalAttributeInteger("maxlength");

    private string RichTextAriaLabel => Label ?? Placeholder ?? FieldIdentifier.FieldName;

    private string RichTextPlaceholder => string.IsNullOrWhiteSpace(Placeholder) ? "Start typing" : Placeholder;

    /// <summary>
    /// Initializes a new instance of the <see cref="NTRichTextEditor" /> class.
    /// </summary>
    public NTRichTextEditor(IJSRuntime jsRuntime) {
        _jsRuntime = jsRuntime;
        DotNetObjectRef = DotNetObjectReference.Create(this);
    }

    private static RenderFragment<bool> CreateToolPanelTemplate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TPanel>() where TPanel : IComponent =>
        disabled => builder => {
            builder.OpenComponent<TPanel>(0);
            builder.AddAttribute(1, "Disabled", disabled);
            builder.CloseComponent();
        };


    private bool HasToolbarAction(string action) {
        foreach (var button in ToolbarButtons ?? DefaultToolbarButtons) {
            if (!button.IsDivider && string.Equals(button.Action, action, StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    private static string GetToolbarButtonAriaLabel(INTRichTextEditorButton toolbarButton) =>
        !string.IsNullOrWhiteSpace(toolbarButton.AriaLabel)
            ? toolbarButton.AriaLabel
            : !string.IsNullOrWhiteSpace(toolbarButton.Text)
                ? toolbarButton.Text
                : toolbarButton.Title ?? toolbarButton.Action ?? "Rich text editor action";

    internal static string GetToolbarButtonTitle(INTRichTextEditorButton toolbarButton) {
        if (!string.IsNullOrWhiteSpace(toolbarButton.Shortcut)) {
            return toolbarButton.Shortcut;
        }

        return !string.IsNullOrWhiteSpace(toolbarButton.Title)
            ? toolbarButton.Title
            : GetToolbarButtonAriaLabel(toolbarButton);
    }

    internal static string? GetToolbarButtonAriaKeyShortcuts(INTRichTextEditorButton toolbarButton) {
        if (string.IsNullOrWhiteSpace(toolbarButton.Shortcut)) {
            return null;
        }

        return _ariaKeyShortcuts.GetOrAdd(toolbarButton.Shortcut, static shortcut => shortcut
            .Replace("Ctrl", "Control", StringComparison.OrdinalIgnoreCase)
            .Replace("Cmd", "Meta", StringComparison.OrdinalIgnoreCase)
            .Replace("Option", "Alt", StringComparison.OrdinalIgnoreCase));
    }

    // Constant for the common case; custom classes still go through the builder so dedupe and trimming stay identical.
    private static string GetToolbarButtonClass(INTRichTextEditorButton toolbarButton) => string.IsNullOrWhiteSpace(toolbarButton.CssClass)
        ? "tnt-components tnt-rich-text-editor-toolbar-button"
        : CssClassBuilder.Create().AddClass("tnt-rich-text-editor-toolbar-button").AddClass(toolbarButton.CssClass).Build();

    private static string GetToolbarDividerClass(INTRichTextEditorButton toolbarButton) => string.IsNullOrWhiteSpace(toolbarButton.CssClass)
        ? "tnt-components tnt-rich-text-editor-toolbar-divider"
        : CssClassBuilder.Create().AddClass("tnt-rich-text-editor-toolbar-divider").AddClass(toolbarButton.CssClass).Build();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (ErrorText is null && ErrorMessage is not null) {
            ErrorText = ErrorMessage;
        }

        if (LeadingIcon is null && StartIcon is not null) {
            LeadingIcon = StartIcon;
        }

        if (TrailingIcon is null && EndIcon is not null) {
            TrailingIcon = EndIcon;
        }

        if (ShowInputLength.HasValue) {
            ShowCharacterCounter = ShowInputLength.Value;
        }

        TooltipIcon.Tooltip = Tooltip;
        TooltipIcon.AdditionalClass = "tnt-tooltip-icon";
        TooltipIcon.Size = IconSize.Small;

        base.OnParametersSet();

        if (ValueExpression is not null) {
            _boundModel = FieldIdentifier.Create(ValueExpression).Model;
        }

        var activeTools = new List<INTRichTextEditorTool>();
        foreach (var tool in Tools ?? DefaultTools) {
            if (HasToolbarAction(tool.Action)) {
                activeTools.Add(tool);
            }
        }

        _activeToolsToRender = activeTools;

        PageScript = CreatePageScript(_activeToolsToRender);
    }

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-rich-text-editor");
        builder.Append(" nt-textarea");

        if (TryGetAdditionalAttribute("class", out var additionalClass) && !string.IsNullOrWhiteSpace(additionalClass?.ToString())) {
            builder.Append(' ');
            builder.Append(additionalClass);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_disposalState.TryBegin()) {
            try {
                await DisposeAsyncCore().ConfigureAwait(false);
            }
            finally {
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <inheritdoc />
    public override async ValueTask SetFocusAsync() {
        if (!RendererInfo.IsInteractive || IsolatedJsModule is null) {
            await base.SetFocusAsync();
            return;
        }

        try {
            await IsolatedJsModule.InvokeVoidAsync("focusEditor", _editorElement);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore.
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        // Static SSR enhancement is handled by NTPageScript in the browser. The .NET callback bridge is only available once the renderer becomes interactive.
        if (RendererInfo.IsInteractive && !DisposalStarted) {
            try {
                if (firstRender) {
                    var importedModule = await _jsRuntime.ImportIsolatedJs(this, JsModulePath);
                    if (!DisposalStarted) {
                        IsolatedJsModule = importedModule;
                        if (TryGetInteropReferences(out var module, out var dotNetRef)) {
                            await module.InvokeVoidAsync("onLoad", _editorElement, dotNetRef);
                        }
                    }
                    else {
                        await importedModule.DisposeAsync().ConfigureAwait(false);
                    }
                }

                if (TryGetInteropReferences(out var currentModule, out var currentDotNetRef)) {
                    await currentModule.InvokeVoidAsync("onUpdate", _editorElement, currentDotNetRef);
                }
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during render.
            }
            catch (Exception exception) when (firstRender && exception is JSException or OperationCanceledException) {
                _scriptLoadFailed = true;
                StateHasChanged();
            }
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    /// <summary>
    /// Synchronizes the editor value from JavaScript while the user is typing.
    /// </summary>
    /// <param name="sequence">The send order of this value; a value older than one already applied is ignored.</param>
    /// <param name="value">The current sanitized HTML value.</param>
    [JSInvokable]
    public async Task UpdateValueFromJs(long sequence, string value) {
        _receivedValueCount++;
        if (!TryAdvance(ref _valueSequence, sequence)) {
            return;
        }

        CurrentValueAsString = value;
        await SetMarkupValueAsync(sequence, value);

        if (BindOnInput) {
            await BindAfter.InvokeAsync(CurrentValue);
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Commits an edited value when the editor loses focus, then completes the blur like <see cref="BlurFromJs" />.
    /// </summary>
    /// <param name="sequence">The send order of this value; a value older than one already applied is not applied.</param>
    /// <param name="value">The current sanitized HTML value.</param>
    [JSInvokable]
    public async Task CommitValueFromJs(long sequence, string value) {
        _receivedValueCount++;
        if (DisposalStarted && IsBindingRetargeted()) {
            return;
        }

        var previousValue = CurrentValueAsString ?? string.Empty;

        // A newer value already applied wins, but the blur still happened, so the blur still completes.
        if (TryAdvance(ref _valueSequence, sequence)) {
            CurrentValueAsString = value;
            await SetMarkupValueAsync(sequence, value);
            if (!string.Equals(previousValue, value, StringComparison.Ordinal)) {
                await BindAfter.InvokeAsync(CurrentValue);
            }
        }

        await BlurFromJs();
    }

    /// <summary>
    /// Completes a blur that carried no edit: marks the field touched for validation, as other inputs do on blur, and raises
    /// <see cref="NTFormControlBaseCore{TValue}.OnBlurCallback" />. The value and <c>BindAfter</c> are left alone.
    /// </summary>
    [JSInvokable]
    public async Task BlurFromJs() {
        // A changed value already notified through CurrentValueAsString, so only an untouched field is notified here.
        if (EditContext is not null && !EditContext.IsModified(FieldIdentifier)) {
            EditContext.NotifyFieldChanged(FieldIdentifier);
        }

        await OnBlurCallback.InvokeAsync(new FocusEventArgs());
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Synchronizes the editor HTML value from JavaScript without changing the bound value.
    /// </summary>
    /// <param name="sequence">The send order of this markup; markup older than markup already applied is ignored.</param>
    /// <param name="html">The current HTML value.</param>
    [JSInvokable]
    public Task UpdateMarkupValueFromJs(long sequence, string html) => SetMarkupValueAsync(sequence, html);

    /// <summary>
    /// Receives a value too large for a single interop message and applies it through <paramref name="methodName" />.
    /// </summary>
    /// <param name="methodName">
    /// <see cref="UpdateValueFromJs" />, <see cref="CommitValueFromJs" />, or <see cref="UpdateMarkupValueFromJs" />.
    /// </param>
    /// <param name="sequence">The send order of this value.</param>
    /// <param name="value">A stream of the UTF-8 encoded value.</param>
    /// <remarks>
    /// A stream larger than <see cref="NTComponentsDefaultOptions.RichTextEditorMaxValueBytes" /> is rejected here, on the server, and not applied.
    /// </remarks>
    [JSInvokable]
    public async Task ReceiveValueStreamFromJs(string methodName, long sequence, IJSStreamReference value) {
        var maxBytes = (Services.GetService<NTComponentsDefaultOptions>() ?? NTComponentsDefaultOptions.Default).RichTextEditorMaxValueBytes;
        string text;
        await using (value) {
            await using var stream = await value.OpenReadStreamAsync(maxBytes);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            text = await reader.ReadToEndAsync();
        }

        // Counted only once read, so a render during the read still reports this value as unseen.
        await (methodName switch {
            nameof(UpdateValueFromJs) => UpdateValueFromJs(sequence, text),
            nameof(CommitValueFromJs) => CommitValueFromJs(sequence, text),
            nameof(UpdateMarkupValueFromJs) => UpdateMarkupValueFromJs(sequence, text),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Unknown editor value method.")
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing && _disposalState.TryBegin()) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Releases the JavaScript module asynchronously.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore() {
        if (IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("onDispose", _editorElement, DotNetObjectRef);
                await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }

            IsolatedJsModule = null;
        }

        DotNetObjectRef?.Dispose();
        DotNetObjectRef = null;
    }

    private bool DisposalStarted => _disposalState.HasStarted;

    // @bind reads its target when invoked, so a flush after the parent moved on would write this text to the wrong record.
    private bool IsBindingRetargeted() {
        if (ValueExpression is null) {
            return false;
        }

        try {
            return !ReferenceEquals(FieldIdentifier.Create(ValueExpression).Model, _boundModel);
        }
        catch (Exception exception) when (exception is ArgumentException or NullReferenceException or InvalidOperationException) {
            return true;
        }
    }

    private bool TryGetInteropReferences([NotNullWhen(true)] out IJSObjectReference? module, [NotNullWhen(true)] out DotNetObjectReference<NTRichTextEditor>? dotNetRef) {
        module = IsolatedJsModule;
        dotNetRef = DotNetObjectRef;
        return !DisposalStarted && module is not null && dotNetRef is not null;
    }

    // Markup has its own sequence because a markup-only update must not make an older pending value look superseded.
    private async Task SetMarkupValueAsync(long sequence, string? html) {
        var nextMarkupValue = new MarkupString(html ?? string.Empty);
        if (!TryAdvance(ref _markupSequence, sequence) || string.Equals(_markupValue.Value, nextMarkupValue.Value, StringComparison.Ordinal)) {
            return;
        }

        _markupValue = nextMarkupValue;
        await MarkupValueChanged.InvokeAsync(_markupValue);
    }

    private static bool TryAdvance(ref long appliedSequence, long sequence) {
        if (sequence <= appliedSequence) {
            return false;
        }

        appliedSequence = sequence;
        return true;
    }

    private int? TryParseAdditionalAttributeInteger(string name) {
        if (!TryGetAdditionalAttribute(name, out var value)) {
            return null;
        }

        return int.TryParse(value?.ToString(), out var result) ? result : null;
    }

    private RenderFragment CreatePageScript(IReadOnlyList<INTRichTextEditorTool> activeToolsToRender) => builder => {
        builder.OpenComponent<NTPageScript>(0);
        builder.AddAttribute(1, nameof(NTPageScript.Src), JsModulePath);
        builder.CloseComponent();

        var sequence = 2;
        HashSet<string>? seen = null;
        foreach (var tool in activeToolsToRender) {
            var jsModulePath = tool.JsModulePath;
            if (!string.IsNullOrWhiteSpace(jsModulePath) && (seen ??= new HashSet<string>(StringComparer.Ordinal)).Add(jsModulePath)) {
                builder.OpenComponent<NTPageScript>(sequence++);
                builder.AddAttribute(sequence++, nameof(NTPageScript.Src), jsModulePath);
                builder.CloseComponent();
            }
        }
    };
}
