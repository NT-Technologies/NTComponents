using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using NTComponents.Core;

namespace NTComponents.Tests.Editors;

/// <summary>
///     Unit tests for <see cref="NTRichTextEditor" />.
/// </summary>
public class NTRichTextEditor_Tests : BunitContext {
    private readonly BunitJSModuleInterop _module;
    private readonly NTComponentsDefaultOptions _options = new();

    public NTRichTextEditor_Tests() {
        Services.AddSingleton(_options);
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        _module = JSInterop.SetupModule("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
        _module.SetupVoid("onLoad", _ => true).SetVoidResult();
        _module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        _module.SetupVoid("onDispose", _ => true).SetVoidResult();
        _module.SetupVoid("focusEditor", _ => true).SetVoidResult();
        var tooltipModule = JSInterop.SetupModule("./_content/NTComponents/Tooltip/NTTooltip.razor.js");
        tooltipModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        tooltipModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        tooltipModule.SetupVoid("onDispose", _ => true).SetVoidResult();
        SetupTableMenuModules(this);
    }

    // The table tool renders an NTContextMenu (and its NTMenu), which load their own modules.
    private static void SetupTableMenuModules(BunitContext context) {
        foreach (var path in new[] { "./_content/NTComponents/Menus/NTContextMenu.razor.js", "./_content/NTComponents/Menus/NTMenu.razor.js" }) {
            var menuModule = context.JSInterop.SetupModule(path);
            menuModule.SetupVoid("onLoad", _ => true).SetVoidResult();
            menuModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
            menuModule.SetupVoid("onDispose", _ => true).SetVoidResult();
        }
    }

    [Fact]
    public void Component_Implements_PageScript_Component_Interface() {
        var cut = RenderEditor();

        cut.Instance.Should().BeAssignableTo<NTComponents.Interfaces.INTPageScriptComponent<NTRichTextEditor>>();
        cut.Instance.Should().BeAssignableTo<IAsyncDisposable>();
        cut.Instance.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void JsModulePath_Returns_Correct_Path() {
        var cut = RenderEditor();

        cut.Instance.JsModulePath.Should().Be("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
    }

    [Fact]
    public void Type_Returns_TextArea() {
        var cut = RenderEditor();

        cut.Instance.Type.Should().Be(InputType.TextArea);
    }

    [Fact]
    public void PageScript_RenderFragment_Is_Included() {
        var cut = RenderEditor();

        var pageScripts = cut.FindAll("tnt-page-script").Select(script => script.GetAttribute("src")).ToList();
        pageScripts.Should().ContainSingle(src => src == "./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
        pageScripts.Where(src => src != "./_content/NTComponents/Editors/NTRichTextEditor.razor.js").Should().OnlyContain(src => src == "./_content/NTComponents/Form/NTInputColor.razor.js" || src == "./_content/NTComponents/Menus/NTContextMenu.razor.js" || src == "./_content/NTComponents/Menus/NTMenu.razor.js", "the tool panels add only the color field and table context menu modules");
    }

    [Fact]
    public void Renders_Rich_Text_Editor_Structure() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Label, "Body")
            .Add(x => x.Placeholder, "Write something"));

        cut.Find("nt-rich-text-editor").Should().NotBeNull();
        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-button").Should().HaveCount(28);
        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-divider").Should().HaveCount(6);
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='undo']").GetAttribute("title").Should().Be("Ctrl+Z");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='redo']").GetAttribute("title").Should().Be("Ctrl+Y");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']").GetAttribute("title").Should().Be("Ctrl+B");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='unorderedList']").GetAttribute("title").Should().Be("Ctrl+Alt+7");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='strikeThrough']").GetAttribute("title").Should().Be("Ctrl+Shift+S");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='textColor']").GetAttribute("title").Should().Be("Ctrl+Alt+X");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='link']").GetAttribute("title").Should().Be("Ctrl+K");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='alignCenter']").GetAttribute("title").Should().Be("Ctrl+Shift+E");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='image']").GetAttribute("title").Should().Be("Ctrl+Alt+M");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='table']").GetAttribute("title").Should().Be("Ctrl+Alt+T");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='iframe']").GetAttribute("title").Should().Be("Ctrl+Alt+F");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='ariaLabel']").GetAttribute("aria-haspopup").Should().Be("dialog");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='editHtml']").GetAttribute("aria-label").Should().Be("Edit HTML source");
        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-icon").Should().OnlyContain(icon => !icon.HasAttribute("title"));
        cut.Find("[data-role='image-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='image']").Should().NotBeNull();
        cut.Find("[data-role='image-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='image-file']").GetAttribute("type").Should().Be("file");
        cut.Find("[data-role='image-title']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='image-width']").GetAttribute("type").Should().Be("number");
        cut.Find("[data-role='image-height']").GetAttribute("type").Should().Be("number");
        cut.Find("[data-role='table-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='table']").Should().NotBeNull();
        cut.FindAll("[data-role='table-grid'] button[data-col][data-row]").Should().HaveCount(80);
        cut.Find("[data-role='table-border-color']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='table-caption']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='text-color-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='textColor']").Should().NotBeNull();
        cut.Find("[data-role='text-color-value']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='link-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='link']").Should().NotBeNull();
        cut.Find("[data-role='link-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='link-text']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='link-aria-label']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='link-title']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-tool-command='ariaLabel']").GetAttribute("role").Should().Be("dialog");
        cut.Find("[data-role='aria-label-value']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='iframe']").Should().NotBeNull();
        cut.Find("[data-role='iframe-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='iframe-title']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-width']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-height']").GetAttribute("type").Should().Be("text");
        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("type").Should().Be("hidden");
        cut.Find("[data-role='html-source']").HasAttribute("hidden").Should().BeTrue();
        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("name").Should().Contain(nameof(RichTextEditorModel.Value));
        cut.FindAll("[data-tool-command] input").Should().OnlyContain(input => !input.HasAttribute("name"));
        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("data-placeholder").Should().Be("Write something");
    }

    [Fact]
    public void Interactive_Renderer_Leaves_Html_Rendering_To_JavaScript() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Value, "<p>This is <strong>bold</strong>, <em>italic</em>, <u>underline</u>, <s>crossed out</s>, <span style=\"color:#2563eb;\">blue text</span>, <a href=\"https://example.com\">linked</a>, and a table.</p><table><thead><tr><th>Name</th><th>Role</th></tr></thead><tbody><tr><td>Avery</td><td>Host</td></tr></tbody></table><iframe src=\"https://example.com/embed\" title=\"Example embed\" width=\"100%\" height=\"315\" loading=\"lazy\"></iframe>"));

        cut.Find(".tnt-rich-text-editor-surface").InnerHtml.Should().BeEmpty();
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Contain("<strong>bold</strong>");
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Contain("<table>");
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Contain("<iframe src=\"https://example.com/embed\"");
    }

    [Fact]
    public void StaticRenderer_Renders_Enhanced_Markup_For_Ssr() {
        SetRendererInfo(new RendererInfo("Static", false));

        var model = new RichTextEditorModel();
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.Value, "<h2>SSR HTML</h2>")
            .Add(x => x.Placeholder, "Write HTML"));

        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("name").Should().Contain(nameof(RichTextEditorModel.Value));
        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("value").Should().Be("<h2>SSR HTML</h2>");
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Be("<h2>SSR HTML</h2>");
        cut.Find(".tnt-rich-text-editor-toolbar").Should().NotBeNull();
        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("data-placeholder").Should().Be("Write HTML");
        cut.FindAll("textarea.tnt-rich-text-editor-ssr-input").Should().BeEmpty();
    }

    [Fact]
    public void Renders_Html_Source_Mode() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Value, "<p>HTML source</p>"));

        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("value").Should().Be("<p>HTML source</p>");
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Be("<p>HTML source</p>");
    }

    [Fact]
    public void Interactive_Surface_Renders_ReadOnly_Until_The_Script_Enables_It() {
        var cut = RenderEditor();

        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("contenteditable").Should().Be("false");
        cut.Find("nt-rich-text-editor").GetAttribute("data-editable").Should().Be("true");
        cut.Find("nt-rich-text-editor").GetAttribute("data-interactive").Should().Be("true");
    }

    [Fact]
    public void Toolbar_And_Accessibility_State_Render_ReadOnly_Until_The_Script_Enables_Them() {
        var cut = RenderEditor();

        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("aria-readonly").Should().Be("true");
        cut.FindAll(".tnt-rich-text-editor-toolbar-button").Should().OnlyContain(button => button.HasAttribute("disabled"));
    }

    [Fact]
    public void Static_Render_Lets_The_Script_Enable_The_Surface_Without_DotNet() {
        SetRendererInfo(new RendererInfo("Static", false));

        var cut = RenderEditor();

        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("contenteditable").Should().Be("false");
        cut.Find("nt-rich-text-editor").GetAttribute("data-interactive").Should().Be("false");
    }

    [Fact]
    public void AdditionalAttributes_Cannot_Override_Script_Contract_Attributes() {
        var cut = RenderEditor(parameters => parameters
            .AddUnmatched("data-interactive", "false")
            .AddUnmatched("data-received-value-count", "99"));

        cut.Find("nt-rich-text-editor").GetAttribute("data-interactive").Should().Be("true");
        cut.Find("nt-rich-text-editor").GetAttribute("data-received-value-count").Should().Be("0");
    }

    [Fact]
    public async Task Render_Reports_Every_Value_Received_From_JavaScript() {
        var cut = RenderEditor(parameters => parameters.Add(x => x.BindOnInput, true));

        await cut.InvokeAsync(() => cut.Instance.UpdateValueFromJs(1, "<p>A</p>"));
        await cut.InvokeAsync(() => cut.Instance.CommitValueFromJs(2, "<p>AB</p>"));

        cut.Find("nt-rich-text-editor").GetAttribute("data-received-value-count").Should().Be("2");
    }

    [Fact]
    public async Task CommitValueFromJs_During_Disposal_Still_Delivers_The_Value_To_The_Parent() {
        var boundValue = "<p>Start</p>";
        var model = new RichTextEditorModel();
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.Value, boundValue)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, value => boundValue = value ?? string.Empty)));
        var pendingOnDispose = _module.SetupVoid("onDispose", _ => true);

        var disposal = cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        await cut.InvokeAsync(() => cut.Instance.CommitValueFromJs(1, "<p>Unsaved</p>"));
        pendingOnDispose.SetVoidResult();
        await disposal;

        boundValue.Should().Be("<p>Unsaved</p>");
    }

    [Fact]
    public async Task CommitValueFromJs_During_Disposal_Skips_A_Binding_That_Moved_To_Another_Record() {
        var holder = new SelectedNote { Note = new RichTextEditorModel { Value = "<p>First</p>" } };
        var delivered = new List<string?>();
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => holder.Note!.Value)
            .Add(x => x.Value, holder.Note!.Value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, delivered.Add)));
        var pendingOnDispose = _module.SetupVoid("onDispose", _ => true);

        var disposal = cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        holder.Note = new RichTextEditorModel { Value = "<p>Second</p>" };
        await cut.InvokeAsync(() => cut.Instance.CommitValueFromJs(1, "<p>Unsaved</p>"));
        pendingOnDispose.SetVoidResult();
        await disposal;

        delivered.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitValueFromJs_During_Disposal_Skips_A_Binding_That_No_Longer_Resolves() {
        var holder = new SelectedNote { Note = new RichTextEditorModel { Value = "<p>First</p>" } };
        var delivered = new List<string?>();
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => holder.Note!.Value)
            .Add(x => x.Value, holder.Note!.Value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, delivered.Add)));
        var pendingOnDispose = _module.SetupVoid("onDispose", _ => true);

        var disposal = cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        holder.Note = null;
        await cut.InvokeAsync(() => cut.Instance.CommitValueFromJs(1, "<p>Unsaved</p>"));
        pendingOnDispose.SetVoidResult();
        await disposal;

        delivered.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitValueFromJs_Before_Disposal_Follows_A_Retargeted_Binding() {
        var holder = new SelectedNote { Note = new RichTextEditorModel { Value = "<p>First</p>" } };
        var delivered = new List<string?>();
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => holder.Note!.Value)
            .Add(x => x.Value, holder.Note!.Value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, delivered.Add)));

        holder.Note = new RichTextEditorModel { Value = "<p>Second</p>" };
        await cut.InvokeAsync(() => cut.Instance.CommitValueFromJs(1, "<p>Typed</p>"));

        delivered.Should().ContainSingle().Which.Should().Be("<p>Typed</p>");
    }

    [Fact]
    public void Script_That_Fails_To_Load_Shows_An_Error_Instead_Of_Crashing_The_Renderer() {
        _module.SetupVoid("onLoad", _ => true).SetException(new JSException("Failed to fetch dynamically imported module"));

        var cut = RenderEditor();

        cut.WaitForAssertion(() => cut.Find(".nt-input-error-text").TextContent.Should().Contain("could not load"));
        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("contenteditable").Should().Be("false");
    }

    [Fact]
    public async Task UpdateValueFromJs_Updates_Value_And_Fires_BindAfter_When_BindOnInput_Is_True() {
        var bindAfterValue = string.Empty;
        var markupValue = string.Empty;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.BindOnInput, true)
            .Add(x => x.MarkupValueChanged, EventCallback.Factory.Create<MarkupString>(this, value => markupValue = value.Value))
            .Add(x => x.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValue = value ?? string.Empty)));

        await cut.Instance.UpdateValueFromJs(1, "<p>Updated <strong>HTML</strong></p>");

        cut.Instance.Value.Should().Be("<p>Updated <strong>HTML</strong></p>");
        cut.Instance.MarkupValue.Value.Should().Be("<p>Updated <strong>HTML</strong></p>");
        markupValue.Should().Be("<p>Updated <strong>HTML</strong></p>");
        bindAfterValue.Should().Be("<p>Updated <strong>HTML</strong></p>");
    }

    [Fact]
    public async Task CommitValueFromJs_Notifies_EditContext_And_Blur_Callback() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);
        var notifiedFields = new List<string>();
        var blurInvoked = false;
        var markupValue = string.Empty;

        editContext.OnFieldChanged += (_, args) => notifiedFields.Add(args.FieldIdentifier.FieldName);

        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.MarkupValueChanged, EventCallback.Factory.Create<MarkupString>(this, value => markupValue = value.Value))
            .Add(x => x.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => blurInvoked = true))
            .AddCascadingValue(editContext));

        await cut.Instance.CommitValueFromJs(1, "<p>Committed</p>");

        notifiedFields.Should().Contain(nameof(RichTextEditorModel.Value));
        blurInvoked.Should().BeTrue();
        cut.Instance.Value.Should().Be("<p>Committed</p>");
        cut.Instance.MarkupValue.Value.Should().Be("<p>Committed</p>");
        markupValue.Should().Be("<p>Committed</p>");
    }

    [Fact]
    public async Task UpdateMarkupValueFromJs_Updates_Markup_Without_Changing_Bound_Value() {
        var cut = RenderEditor(parameters => parameters.Add(x => x.Value, "Original"));

        await cut.Instance.UpdateMarkupValueFromJs(1, "<p>Preview</p>");

        cut.Instance.Value.Should().Be("Original");
        cut.Instance.MarkupValue.Value.Should().Be("<p>Preview</p>");
    }

    [Fact]
    public async Task UpdateMarkupValueFromJs_Does_Not_Reemit_Unchanged_Markup() {
        var markupChangedCount = 0;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.MarkupValueChanged, EventCallback.Factory.Create<MarkupString>(this, _ => markupChangedCount++)));

        await cut.Instance.UpdateMarkupValueFromJs(1, "<p>Preview</p>");
        await cut.Instance.UpdateMarkupValueFromJs(2, "<p>Preview</p>");

        markupChangedCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateMarkupValueFromJs_Normalizes_Null_To_Empty_Markup() {
        var cut = RenderEditor();

        await cut.Instance.UpdateMarkupValueFromJs(1, null!);

        cut.Instance.MarkupValue.Value.Should().Be(string.Empty);
    }

    [Fact]
    public async Task CommitValueFromJs_Skips_BindAfter_When_BindOnInput_And_Value_Is_Unchanged() {
        var bindAfterCount = 0;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.BindOnInput, true)
            .Add(x => x.Value, "Same value")
            .Add(x => x.BindAfter, EventCallback.Factory.Create<string?>(this, _ => bindAfterCount++)));

        await cut.Instance.CommitValueFromJs(1, "Same value");

        bindAfterCount.Should().Be(0);
    }

    [Fact]
    public async Task ReceiveValueStreamFromJs_Commit_Applies_The_Decoded_Value_And_Notifies_EditContext() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);
        var notifiedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => notifiedFields.Add(args.FieldIdentifier.FieldName);
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .AddCascadingValue(editContext));
        var value = string.Concat(Enumerable.Repeat("<p>Größe 文字</p>", 2000));

        await cut.InvokeAsync(() => cut.Instance.ReceiveValueStreamFromJs(nameof(NTRichTextEditor.CommitValueFromJs), 1, CreateStreamReference(Task.FromResult(Utf8Stream(value)))));

        cut.Instance.Value.Should().Be(value);
        cut.Instance.MarkupValue.Value.Should().Be(value);
        notifiedFields.Should().Contain(nameof(RichTextEditorModel.Value));
    }

    [Fact]
    public async Task ReceiveValueStreamFromJs_Finishing_After_A_Newer_Value_Does_Not_Overwrite_It() {
        var pendingStream = new TaskCompletionSource<Stream>();
        var cut = RenderEditor(parameters => parameters.Add(x => x.BindOnInput, true));

        var streamed = cut.InvokeAsync(() => cut.Instance.ReceiveValueStreamFromJs(nameof(NTRichTextEditor.UpdateValueFromJs), 1, CreateStreamReference(pendingStream.Task)));
        await cut.InvokeAsync(() => cut.Instance.UpdateValueFromJs(2, "<p>Newer</p>"));
        pendingStream.SetResult(Utf8Stream("<p>Older</p>"));
        await streamed;

        cut.Instance.Value.Should().Be("<p>Newer</p>");
        cut.Instance.MarkupValue.Value.Should().Be("<p>Newer</p>");
    }

    [Fact]
    public async Task ReceiveValueStreamFromJs_Counts_A_Value_As_Received_Only_Once_It_Is_Read() {
        var pendingStream = new TaskCompletionSource<Stream>();
        var cut = RenderEditor(parameters => parameters.Add(x => x.BindOnInput, true));

        var streamed = cut.InvokeAsync(() => cut.Instance.ReceiveValueStreamFromJs(nameof(NTRichTextEditor.UpdateValueFromJs), 1, CreateStreamReference(pendingStream.Task)));
        await cut.InvokeAsync(() => cut.Instance.UpdateValueFromJs(2, "<p>Newer</p>"));
        var countWhileReading = cut.Find("nt-rich-text-editor").GetAttribute("data-received-value-count");
        pendingStream.SetResult(Utf8Stream("<p>Older</p>"));
        await streamed;
        cut.Render();

        countWhileReading.Should().Be("1");
        cut.Find("nt-rich-text-editor").GetAttribute("data-received-value-count").Should().Be("2");
    }

    [Fact]
    public async Task ReceiveValueStreamFromJs_Rejects_An_Unknown_Method_Name() {
        var cut = RenderEditor();

        var receive = () => cut.InvokeAsync(() => cut.Instance.ReceiveValueStreamFromJs("Unknown", 1, CreateStreamReference(Task.FromResult(Utf8Stream("<p>A</p>")))));

        await receive.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CommitValueFromJs_Older_Than_An_Applied_Update_Keeps_The_Newer_Value_And_Still_Raises_Blur() {
        var blurInvoked = false;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.BindOnInput, true)
            .Add(x => x.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => blurInvoked = true)));

        await cut.InvokeAsync(() => cut.Instance.UpdateValueFromJs(2, "<p>Newer</p>"));
        await cut.InvokeAsync(() => cut.Instance.CommitValueFromJs(1, "<p>Older</p>"));

        cut.Instance.Value.Should().Be("<p>Newer</p>");
        blurInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task CommitValueFromJs_Skips_BindAfter_When_The_Value_Is_Unchanged_Without_BindOnInput() {
        var bindAfterCount = 0;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Value, "Same value")
            .Add(x => x.BindAfter, EventCallback.Factory.Create<string?>(this, _ => bindAfterCount++)));

        await cut.InvokeAsync(() => cut.Instance.CommitValueFromJs(1, "Same value"));

        bindAfterCount.Should().Be(0);
    }

    [Fact]
    public async Task BlurFromJs_On_An_Untouched_Field_Marks_It_Touched_And_Raises_Blur_Without_BindAfter() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);
        var notifiedFields = new List<string>();
        var blurInvoked = false;
        var bindAfterCount = 0;
        editContext.OnFieldChanged += (_, args) => notifiedFields.Add(args.FieldIdentifier.FieldName);
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => blurInvoked = true))
            .Add(x => x.BindAfter, EventCallback.Factory.Create<string?>(this, _ => bindAfterCount++))
            .AddCascadingValue(editContext));

        await cut.InvokeAsync(() => cut.Instance.BlurFromJs());

        notifiedFields.Should().ContainSingle().Which.Should().Be(nameof(RichTextEditorModel.Value));
        blurInvoked.Should().BeTrue();
        bindAfterCount.Should().Be(0);
    }

    [Fact]
    public async Task BlurFromJs_On_A_Modified_Field_Does_Not_Notify_Again() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .AddCascadingValue(editContext));
        await cut.InvokeAsync(() => cut.Instance.BlurFromJs());
        var notifiedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => notifiedFields.Add(args.FieldIdentifier.FieldName);

        await cut.InvokeAsync(() => cut.Instance.BlurFromJs());

        notifiedFields.Should().BeEmpty();
    }

    [Fact]
    public async Task ReceiveValueStreamFromJs_Reads_At_Most_The_Server_Configured_Limit() {
        _options.RichTextEditorMaxValueBytes = 1234;
        var cut = RenderEditor();
        var reference = CreateStreamReference(Task.FromResult(Utf8Stream("<p>A</p>")));

        await cut.InvokeAsync(() => cut.Instance.ReceiveValueStreamFromJs(nameof(NTRichTextEditor.UpdateValueFromJs), 1, reference));

        await reference.Received(1).OpenReadStreamAsync(1234, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMarkupValueFromJs_Does_Not_Make_An_Older_Pending_Value_Look_Superseded() {
        var cut = RenderEditor();

        await cut.InvokeAsync(() => cut.Instance.UpdateMarkupValueFromJs(2, "<p>Normalized</p>"));
        await cut.InvokeAsync(() => cut.Instance.UpdateValueFromJs(1, "<p>Typed</p>"));

        cut.Instance.Value.Should().Be("<p>Typed</p>");
        cut.Instance.MarkupValue.Value.Should().Be("<p>Normalized</p>");
    }

    private static Stream Utf8Stream(string value) => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value));

    private static IJSStreamReference CreateStreamReference(Task<Stream> stream) {
        var reference = Substitute.For<IJSStreamReference>();
        reference.OpenReadStreamAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<Stream>(stream));
        return reference;
    }

    [Fact]
    public void SupportingText_And_ErrorMessage_Render() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.SupportingText, "Helpful text")
            .Add(x => x.ErrorMessage, "Broken"));

        cut.Find(".nt-input-error-text").TextContent.Should().Contain("Broken");
        cut.FindAll(".nt-input-supporting:not(.nt-input-error-text)").Should().BeEmpty();
    }

    [Fact]
    public async Task ValidationMessage_Renders_When_EditContext_Present() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);

        var messageStore = new ValidationMessageStore(editContext);
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .AddCascadingValue(editContext));

        await cut.InvokeAsync(() => {
            messageStore.Add(new FieldIdentifier(model, nameof(RichTextEditorModel.Value)), "Required");
            editContext.NotifyValidationStateChanged();
        });

        cut.WaitForAssertion(() => cut.Find(".nt-input-error-text").TextContent.Should().Contain("Required"));
    }

    [Fact]
    public async Task InvalidClass_Applies_From_EditContext() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        var fieldIdentifier = new FieldIdentifier(model, nameof(RichTextEditorModel.Value));

        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .AddCascadingValue(editContext));

        await cut.InvokeAsync(() => {
            messageStore.Add(fieldIdentifier, "Required");
            editContext.NotifyFieldChanged(fieldIdentifier);
            editContext.NotifyValidationStateChanged();
        });

        cut.WaitForAssertion(() => cut.Find(".nt-input.nt-rich-text-editor").ClassList.Should().Contain("nt-invalid"));
    }

    [Fact]
    public void HiddenInput_Uses_Bound_Field_Name() {
        var model = new RichTextEditorModel();

        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value));

        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("name").Should().Contain(nameof(RichTextEditorModel.Value));
    }

    [Fact]
    public void AdditionalAttributes_Filter_FrameworkManaged_Fields_But_Preserve_Custom_Attributes() {
        var cut = RenderEditor(parameters => parameters
            .AddUnmatched("data-custom", "kept")
            .AddUnmatched("placeholder", "filtered")
            .AddUnmatched("class", "filtered-class")
            .AddUnmatched("maxlength", "25"));

        var editor = cut.Find("nt-rich-text-editor");
        editor.GetAttribute("data-custom").Should().Be("kept");
        editor.HasAttribute("placeholder").Should().BeFalse();
        editor.HasAttribute("maxlength").Should().BeFalse();
        cut.Find(".nt-input.nt-rich-text-editor").ClassName.Should().Contain("filtered-class");
    }

    [Fact]
    public void Renders_Custom_Toolbar_Buttons_From_Parameter() {
        IReadOnlyList<INTRichTextEditorButton> toolbarButtons = new INTRichTextEditorButton[] {
            new NTRichTextEditorButton("bold", "Custom Bold", "Custom bold", text: "Bold"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("heading", "Custom Heading", "Custom heading", icon: MaterialIcon.LooksTwo, value: "2")
        };

        var cut = RenderEditor(parameters => parameters.Add(x => x.ToolbarButtons, toolbarButtons));

        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-button").Should().HaveCount(2);
        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-divider").Should().HaveCount(1);
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']").TextContent.Should().Contain("Bold");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='heading']").GetAttribute("data-value").Should().Be("2");
        cut.FindAll(".tnt-rich-text-editor-toolbar-button[data-command='undo']").Should().BeEmpty();
    }

    [Fact]
    public void Renders_DefaultPlaceholder_Disabled_Icons_Tooltip_And_InputLength() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Disabled, true)
            .Add(x => x.StartIcon, MaterialIcon.Search)
            .Add(x => x.EndIcon, MaterialIcon.Close)
            .Add(x => x.Tooltip, builder => builder.AddContent(0, "Help"))
            .Add(x => x.Value, "Text")
            .AddUnmatched("maxlength", "10"));

        cut.Find(".nt-input-leading").Should().NotBeNull();
        cut.Find(".nt-input-trailing").Should().NotBeNull();
        cut.Find(".tnt-tooltip-icon").Should().NotBeNull();

        var surface = cut.Find(".tnt-rich-text-editor-surface");
        surface.GetAttribute("data-placeholder").Should().Be("Start typing");
        surface.GetAttribute("contenteditable").Should().Be("false");
        surface.GetAttribute("tabindex").Should().Be("-1");
        surface.GetAttribute("aria-label").Should().Contain(nameof(RichTextEditorModel.Value));

        cut.Find(".nt-input-counter").TextContent.Should().Be("4/10");
        cut.FindAll(".tnt-rich-text-editor-toolbar-button").Should().OnlyContain(button => button.HasAttribute("disabled"));
        cut.Find("[data-role='image-url']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Toolbar_Uses_AriaLabel_Title_And_Action_Fallbacks() {
        IReadOnlyList<INTRichTextEditorButton> toolbarButtons = new INTRichTextEditorButton[] {
            new NTRichTextEditorButton("bold", "Visible Title", "Explicit aria", text: "Bold"),
            new TestToolbarButton {
                Action = "italic",
                Title = null,
                AriaLabel = null,
                Text = "Italic"
            },
            new TestToolbarButton {
                Action = "underline",
                Title = null,
                AriaLabel = null,
                Icon = MaterialIcon.FormatUnderlined
            },
            new TestToolbarButton {
                Action = null,
                Title = null,
                AriaLabel = null,
                Icon = MaterialIcon.Help,
                CssClass = "toolbar-fallback-button"
            }
        };

        var cut = RenderEditor(parameters => parameters.Add(x => x.ToolbarButtons, toolbarButtons));

        var explicitAriaButton = cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']");
        explicitAriaButton.GetAttribute("aria-label").Should().Be("Explicit aria");
        explicitAriaButton.GetAttribute("title").Should().Be("Visible Title");

        var textFallbackButton = cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='italic']");
        textFallbackButton.GetAttribute("aria-label").Should().Be("Italic");
        textFallbackButton.GetAttribute("title").Should().Be("Italic");

        var actionFallbackButton = cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='underline']");
        actionFallbackButton.GetAttribute("aria-label").Should().Be("underline");
        actionFallbackButton.GetAttribute("title").Should().Be("underline");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='underline'] .tnt-rich-text-editor-toolbar-icon").HasAttribute("title").Should().BeFalse();

        var defaultFallbackButton = cut.Find(".toolbar-fallback-button");
        defaultFallbackButton.GetAttribute("aria-label").Should().Be("Rich text editor action");
        defaultFallbackButton.GetAttribute("title").Should().Be("Rich text editor action");
        cut.Find(".toolbar-fallback-button .tnt-rich-text-editor-toolbar-icon").HasAttribute("title").Should().BeFalse();
    }

    [Fact]
    public void Filters_Rendered_Tools_To_Matching_Toolbar_Actions_And_Deduplicates_PageScripts() {
        IReadOnlyList<INTRichTextEditorButton> toolbarButtons = new INTRichTextEditorButton[] {
            new NTRichTextEditorButton("bold", "Bold", "Bold", text: "Bold"),
            new NTRichTextEditorButton("image", "Image", "Image", text: "Image")
        };

        IReadOnlyList<INTRichTextEditorTool> tools = new INTRichTextEditorTool[] {
            new NTRichTextEditorTool("image", disabled => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-tool-command", "image");
                builder.AddAttribute(2, "data-test-tool", "image");
                builder.CloseElement();
            }, "./_content/NTComponents/Editors/Tool/TestSharedTool.razor.js"),
            new NTRichTextEditorTool("link", disabled => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-tool-command", "link");
                builder.AddAttribute(2, "data-test-tool", "link");
                builder.CloseElement();
            }, "./_content/NTComponents/Editors/Tool/TestSharedTool.razor.js")
        };

        var cut = RenderEditor(parameters => parameters
            .Add(x => x.ToolbarButtons, toolbarButtons)
            .Add(x => x.Tools, tools));

        cut.FindAll("[data-test-tool='image']").Should().HaveCount(1);
        cut.FindAll("[data-test-tool='link']").Should().BeEmpty();

        var pageScripts = cut.FindAll("tnt-page-script");
        pageScripts.Select(script => script.GetAttribute("src")).Should().BeEquivalentTo([
            "./_content/NTComponents/Editors/NTRichTextEditor.razor.js",
            "./_content/NTComponents/Editors/Tool/TestSharedTool.razor.js"
        ]);
    }

    [Fact]
    public void Renders_Normalized_AriaKeyShortcuts_For_Custom_Toolbar_Buttons() {
        IReadOnlyList<INTRichTextEditorButton> toolbarButtons = new INTRichTextEditorButton[] {
            new NTRichTextEditorButton("bold", "Custom Bold", "Custom bold", text: "Bold", shortcut: "Cmd+Option+K")
        };

        var cut = RenderEditor(parameters => parameters.Add(x => x.ToolbarButtons, toolbarButtons));

        var toolbarButton = cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']");
        toolbarButton.GetAttribute("aria-keyshortcuts").Should().Be("Meta+Alt+K");
        toolbarButton.GetAttribute("title").Should().Be("Cmd+Option+K");
    }

    [Fact]
    public async Task DisposeAsync_Calls_OnDispose_And_Clears_Interop_State() {
        var cut = RenderEditor();
        var instance = cut.Instance;

        DotNetRef(instance).Should().NotBeNull();
        instance.IsolatedJsModule.Should().NotBeNull();

        await instance.DisposeAsync();

        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "onDispose");
        instance.IsolatedJsModule.Should().BeNull();
        DotNetRef(instance).Should().BeNull();
    }

    [Fact]
    public async Task Disposal_While_OnLoad_Is_Awaiting_Does_Not_Invoke_OnUpdate_With_A_Disposed_Reference() {
        var model = new RichTextEditorModel();
        var cut = Render<TestableRichTextEditor>(parameters => parameters.Add(component => component.ValueExpression, () => model.Value));
        var initialUpdateCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "onUpdate");
        var pendingOnLoad = _module.SetupVoid("onLoad", _ => true);

        var renderTask = cut.InvokeAsync(() => cut.Instance.InvokeOnAfterRenderAsync(firstRender: true));
        await Task.Yield();
        pendingOnLoad.Invocations.Should().ContainSingle();
        ((IDisposable)cut.Instance).Dispose();
        pendingOnLoad.SetVoidResult();
        await renderTask;

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "onUpdate").Should().Be(initialUpdateCount);
        JSInterop.Invocations
            .Where(invocation => invocation.Identifier == "onLoad" || invocation.Identifier == "onUpdate")
            .Should().OnlyContain(invocation => invocation.Arguments[1] != null);
    }

    [Fact]
    public async Task DisposeAsync_Without_Module_Still_Clears_DotNetObjectReference() {
        var instance = new NTRichTextEditor(Services.GetRequiredService<IJSRuntime>());

        DotNetRef(instance).Should().NotBeNull();
        instance.IsolatedJsModule.Should().BeNull();

        await instance.DisposeAsync();

        DotNetRef(instance).Should().BeNull();
    }

    [Fact]
    public void Dispose_Synchronously_Clears_DotNetObjectReference() {
        var cut = RenderEditor();
        var instance = cut.Instance;

        DotNetRef(instance).Should().NotBeNull();

        ((IDisposable)instance).Dispose();

        DotNetRef(instance).Should().BeNull();
    }

    [Fact]
    public void Dispose_False_Does_Not_Clear_DotNetObjectReference() {
        var instance = new TestableRichTextEditor(Services.GetRequiredService<IJSRuntime>());

        DotNetRef(instance).Should().NotBeNull();

        instance.InvokeDispose(disposing: false);
        DotNetRef(instance).Should().NotBeNull();

        instance.InvokeDispose(disposing: true);
        DotNetRef(instance).Should().BeNull();

        instance.InvokeDispose(disposing: true);
        DotNetRef(instance).Should().BeNull();
    }

    [Fact]
    public async Task SetFocusAsync_Invokes_FocusEditor_When_Interactive() {
        var cut = RenderEditor();

        await cut.Instance.SetFocusAsync();

        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "focusEditor");
    }

    [Fact]
    public async Task SetFocusAsync_Does_Not_Invoke_FocusEditor_When_Not_Interactive() {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = RenderEditor();

        await cut.Instance.SetFocusAsync();

        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "focusEditor");
    }

    [Fact]
    public async Task SetFocusAsync_Ignores_JsDisconnectedException() {
        var cut = Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => new RichTextEditorModel().Value));
        cut.Instance.SetIsolatedJsModule(new ThrowingJsModule());

        var act = () => cut.Instance.SetFocusAsync().AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_Ignores_JsDisconnectedException_From_Module() {
        var cut = Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => new RichTextEditorModel().Value));
        cut.Instance.SetIsolatedJsModule(new ThrowingJsModule());

        var act = () => cut.Instance.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
        DotNetRef(cut.Instance).Should().BeNull();
        cut.Instance.IsolatedJsModule.Should().BeNull();
    }

    [Fact]
    public async Task OnAfterRenderAsync_Ignores_JsDisconnectedException_From_OnUpdate() {
        var model = new RichTextEditorModel();
        var cut = Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value));
        cut.Instance.SetIsolatedJsModule(new ThrowingJsModule());

        var act = () => cut.Instance.InvokeOnAfterRenderAsync(firstRender: false);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnAfterRenderAsync_Allows_Null_Module_On_Subsequent_Render() {
        var model = new RichTextEditorModel();
        var cut = Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value));
        cut.Instance.ClearIsolatedJsModule();

        var act = () => cut.Instance.InvokeOnAfterRenderAsync(firstRender: false);

        await act.Should().NotThrowAsync();
        cut.Instance.IsolatedJsModule.Should().BeNull();
    }

    [Fact]
    public void OnAfterRenderAsync_Ignores_JsDisconnectedException_During_FirstRender_Load() {
        using var context = new BunitContext();
        context.SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = context.JSInterop.SetupModule("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
        module.SetupVoid("onLoad", _ => true).SetException(new JSDisconnectedException("Disconnected"));
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("focusEditor", _ => true).SetVoidResult();
        SetupTableMenuModules(context);

        var model = new RichTextEditorModel();
        var act = () => context.Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value));

        act.Should().NotThrow();
    }

    private sealed class RichTextEditorModel {
        public string? Value { get; set; }
    }

    private sealed class SelectedNote {
        public RichTextEditorModel? Note { get; set; }
    }

    private sealed class TestToolbarButton : INTRichTextEditorButton {
        public bool IsDivider => false;
        public string? Action { get; init; }
        public string? Value { get; init; }
        public string? Title { get; init; }
        public string? Shortcut { get; init; }
        public string? AriaLabel { get; init; }
        public NTIcon? Icon { get; init; }
        public string? Text { get; init; }
        public string? CssClass { get; init; }
    }

    private sealed class TestableRichTextEditor(IJSRuntime jsRuntime) : NTRichTextEditor(jsRuntime) {
        public void InvokeDispose(bool disposing) => base.Dispose(disposing);
        public Task InvokeOnAfterRenderAsync(bool firstRender) => base.OnAfterRenderAsync(firstRender);

        public void SetIsolatedJsModule(IJSObjectReference jsObjectReference) =>
            typeof(NTRichTextEditor)
                .GetProperty(nameof(IsolatedJsModule))!
                .SetValue(this, jsObjectReference);

        public void ClearIsolatedJsModule() =>
            typeof(NTRichTextEditor)
                .GetProperty(nameof(IsolatedJsModule))!
                .SetValue(this, null);
    }

    private sealed class ThrowingJsModule : IJSObjectReference {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromException<TValue>(new JSDisconnectedException("Disconnected"));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromException<TValue>(new JSDisconnectedException("Disconnected"));
    }

    private static DotNetObjectReference<NTRichTextEditor>? DotNetRef(NTRichTextEditor editor) =>
        ((NTComponents.Interfaces.INTPageScriptComponent<NTRichTextEditor>)editor).DotNetObjectRef;

    private IRenderedComponent<NTRichTextEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<NTRichTextEditor>>? configure = null) {
        var model = new RichTextEditorModel();
        return Render<NTRichTextEditor>(parameters => {
            parameters.Add(x => x.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }
}
