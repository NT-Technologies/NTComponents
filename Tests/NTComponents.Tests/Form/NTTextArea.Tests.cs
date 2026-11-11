using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTTextArea_Tests : BunitContext {
    private readonly BunitJSModuleInterop _module;

    public NTTextArea_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        _module = JSInterop.SetupModule("./_content/NTComponents/Form/NTTextArea.razor.js");
        _module.SetupVoid("onLoad", _ => true).SetVoidResult();
        _module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        _module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    private sealed class RequiredModel {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string? Notes { get; set; }
    }

    private sealed class TestModel {
        public string? Notes { get; set; }
    }

    [Fact]
    public void Component_Implements_PageScript_Component_Interface() {
        var cut = RenderTextArea();

        cut.Instance.Should().BeAssignableTo<NTComponents.Interfaces.INTPageScriptComponent<NTTextArea>>();
        cut.Instance.Should().BeAssignableTo<IAsyncDisposable>();
        cut.Instance.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void JsModulePath_Returns_TextArea_Module_Path() {
        var cut = RenderTextArea();

        cut.Instance.JsModulePath.Should().Be("./_content/NTComponents/Form/NTTextArea.razor.js");
    }

    [Fact]
    public async Task Disposal_While_OnLoad_Is_Awaiting_Does_Not_Invoke_OnUpdate_With_A_Disposed_Reference() {
        var model = new TestModel();
        var cut = Render<TestTextArea>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Notes)
            .Add(component => component.AutoGrow, true));
        cut.Instance.ClearIsolatedJsModule();
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
    public void Renders_Stable_Label_And_TextArea_Association() {
        var model = new TestModel();

        var cut = RenderTextArea(model, parameters => parameters
            .Add(p => p.ElementId, "notes-input")
            .Add(p => p.Label, "Notes")
            .Add(p => p.SupportingText, "Add enough detail for review"));

        var textarea = cut.Find("textarea");
        var label = cut.Find("label.nt-input-container");

        textarea.GetAttribute("id").Should().Be("notes-input");
        label.GetAttribute("for").Should().Be("notes-input");
        textarea.GetAttribute("aria-describedby").Should().Contain("notes-input-supporting");
        cut.Find("#notes-input-supporting").TextContent.Should().Be("Add enough detail for review");
    }

    [Fact]
    public void Generated_Input_Id_Is_Deterministic_For_Field() {
        var model = new TestModel();

        using var first = RenderTextArea(model, parameters => parameters.Add(p => p.Label, "Notes"));
        var firstTextAreaId = first.Find("textarea").GetAttribute("id");
        var firstLabelFor = first.Find("label.nt-input-container").GetAttribute("for");

        firstTextAreaId.Should().NotBeNullOrWhiteSpace();
        firstTextAreaId.Should().StartWith("nt-textarea-model-notes-");
        firstLabelFor.Should().Be(firstTextAreaId);

        using var second = RenderTextArea(model, parameters => parameters.Add(p => p.Label, "Notes"));

        second.Find("textarea").GetAttribute("id").Should().Be(firstTextAreaId);
        second.Find("label.nt-input-container").GetAttribute("for").Should().Be(firstLabelFor);
    }

    [Fact]
    public void Renders_TextArea_With_FormV2_Field_Classes() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.Label, "Notes"));

        cut.Find("textarea").Should().NotBeNull();
        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-textarea");
        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-input-outlined");
        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-input-standard");
    }

    [Fact]
    public void Passes_Through_TextArea_Specific_Attributes() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
            ["cols"] = "40",
            ["wrap"] = "soft",
            ["spellcheck"] = "true",
            ["data-field"] = "notes"
        }));

        var textarea = cut.Find("textarea");

        textarea.GetAttribute("rows").Should().Be("2");
        textarea.GetAttribute("cols").Should().Be("40");
        textarea.GetAttribute("wrap").Should().Be("soft");
        textarea.GetAttribute("spellcheck").Should().Be("true");
        textarea.GetAttribute("data-field").Should().Be("notes");
    }

    [Fact]
    public void Filters_Input_Only_Attributes() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
            ["type"] = "text",
            ["min"] = "1",
            ["max"] = "10"
        }));

        var textarea = cut.Find("textarea");

        textarea.HasAttribute("type").Should().BeFalse();
        textarea.HasAttribute("min").Should().BeFalse();
        textarea.HasAttribute("max").Should().BeFalse();
    }

    [Fact]
    public void Character_Counter_Renders_From_MaxLength() {
        var model = new TestModel { Notes = "hello" };

        var cut = RenderTextArea(model, parameters => parameters.Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["maxlength"] = "50" }));

        AssertCharacterCounter(cut, "5/50", "50");
    }

    [Fact]
    public void Character_Counter_Renders_For_Static_Ssr_Renderer() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel { Notes = "abc" };

        var cut = RenderTextArea(model, parameters => parameters.Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["maxlength"] = "20" }));

        AssertCharacterCounter(cut, "3/20", "20");
    }

    [Fact]
    public void SizeByContent_Adds_FieldSizing_Style_To_TextArea() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.SizeByContent, true));

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-textarea-size-by-content");
        cut.Find("textarea").GetAttribute("style").Should().Be("field-sizing:content;");
    }

    [Fact]
    public void AutoGrow_Is_Enabled_By_Default() {
        var cut = RenderTextArea();

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-textarea-autogrow");
        cut.Find("textarea").GetAttribute("data-nt-textarea-autogrow").Should().Be("true");
        cut.Find("textarea").GetAttribute("data-nt-textarea-min-visible-lines").Should().Be("2");
        cut.Find("textarea").GetAttribute("data-nt-textarea-max-visible-lines").Should().Be("5");
        cut.Find("textarea").GetAttribute("rows").Should().Be("2");
        cut.Find("textarea").HasAttribute("oninput").Should().BeFalse();
        cut.Find("nt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Form/NTTextArea.razor.js");
    }

    [Fact]
    public void AutoGrow_Can_Be_Disabled() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.AutoGrow, false));

        cut.Find(".nt-input").GetAttribute("class").Should().NotContain("nt-textarea-autogrow");
        cut.Find("textarea").HasAttribute("data-nt-textarea-autogrow").Should().BeFalse();
        cut.Find("textarea").HasAttribute("oninput").Should().BeFalse();
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Fact]
    public void SizeByContent_Disables_Javascript_AutoGrow_Contract() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.SizeByContent, true));

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-textarea-size-by-content");
        cut.Find(".nt-input").GetAttribute("class").Should().NotContain("nt-textarea-autogrow");
        cut.Find("textarea").HasAttribute("data-nt-textarea-autogrow").Should().BeFalse();
        cut.Find("textarea").HasAttribute("data-nt-textarea-max-visible-lines").Should().BeFalse();
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Fact]
    public void MaxVisibleLines_Renders_Autosize_Data_Attribute() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.MaxVisibleLines, 6));

        cut.Find("textarea").GetAttribute("data-nt-textarea-max-visible-lines").Should().Be("6");
    }

    [Fact]
    public void MinVisibleLines_Renders_Rows_And_Autosize_Data_Attribute() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.MinVisibleLines, 3));

        cut.Find("textarea").GetAttribute("rows").Should().Be("3");
        cut.Find("textarea").GetAttribute("data-nt-textarea-min-visible-lines").Should().Be("3");
    }

    [Fact]
    public void MinVisibleLines_Overrides_Additional_Rows_Attribute() {
        var cut = RenderTextArea(configure: parameters => parameters
            .Add(p => p.MinVisibleLines, 3)
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["rows"] = "8" }));

        cut.Find("textarea").GetAttribute("rows").Should().Be("3");
    }

    [Fact]
    public void MaxVisibleLines_Null_Allows_Unbounded_Autosize() {
        var cut = RenderTextArea(configure: parameters => parameters.Add(p => p.MaxVisibleLines, null));

        cut.Find("textarea").HasAttribute("data-nt-textarea-max-visible-lines").Should().BeFalse();
    }

    [Fact]
    public void MinVisibleLines_Must_Be_Positive() {
        var render = () => RenderTextArea(configure: parameters => parameters.Add(p => p.MinVisibleLines, 0));

        render.Should().Throw<InvalidOperationException>().WithMessage("*MinVisibleLines*positive*");
    }

    [Fact]
    public void MaxVisibleLines_Must_Be_Positive() {
        var render = () => RenderTextArea(configure: parameters => parameters.Add(p => p.MaxVisibleLines, 0));

        render.Should().Throw<InvalidOperationException>().WithMessage("*MaxVisibleLines*positive*");
    }

    [Fact]
    public void MaxVisibleLines_Must_Not_Be_Less_Than_MinVisibleLines() {
        var render = () => RenderTextArea(configure: parameters => parameters
            .Add(p => p.MinVisibleLines, 4)
            .Add(p => p.MaxVisibleLines, 3));

        render.Should().Throw<InvalidOperationException>().WithMessage("*MaxVisibleLines*greater than or equal to*MinVisibleLines*");
    }

    [Fact]
    public void SizeByContent_Preserves_Custom_TextArea_Style() {
        var cut = RenderTextArea(configure: parameters => parameters
            .Add(p => p.SizeByContent, true)
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["style"] = "min-height:120px;" }));

        cut.Find("textarea").GetAttribute("style").Should().Be("min-height:120px;field-sizing:content;");
    }

    [Fact]
    public void Change_Updates_Value() {
        var model = new TestModel();
        var cut = RenderTextArea(model);

        cut.Find("textarea").Change("New notes");

        model.Notes.Should().Be("New notes");
    }

    [Fact]
    public void Input_Updates_Value_When_BindOnInput_True() {
        var model = new TestModel();
        var cut = RenderTextArea(model, parameters => parameters.Add(p => p.BindOnInput, true));

        cut.Find("textarea").Input("Typing notes");

        model.Notes.Should().Be("Typing notes");
    }

    [Fact]
    public void Required_Supporting_Text_Comes_From_NTForm() {
        var model = new RequiredModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ShowRequiredSupportingText, true)
            .Add(p => p.RequiredSupportingText, "Required for review")
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTTextArea>(0);
                builder.AddAttribute(1, nameof(NTTextArea.Value), model.Notes);
                builder.AddAttribute(2, nameof(NTTextArea.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Notes = value));
                builder.AddAttribute(3, nameof(NTTextArea.ValueExpression), (Expression<Func<string?>>)(() => model.Notes));
                builder.AddAttribute(4, nameof(NTTextArea.AdditionalAttributes), new Dictionary<string, object> { ["required"] = true });
                builder.CloseComponent();
            }));

        cut.Find(".nt-input-supporting").TextContent.Should().Be("Required for review");
        cut.Find("textarea").HasAttribute("required").Should().BeTrue();
    }

    [Fact]
    public void Blur_Validates_Required_Field_And_Renders_Error_State() {
        var model = new RequiredModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTTextArea>(1);
                builder.AddAttribute(2, nameof(NTTextArea.Value), model.Notes);
                builder.AddAttribute(3, nameof(NTTextArea.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Notes = value));
                builder.AddAttribute(4, nameof(NTTextArea.ValueExpression), (Expression<Func<string?>>)(() => model.Notes));
                builder.CloseComponent();
            }));

        cut.Find(".nt-input").GetAttribute("class").Should().NotContain("nt-invalid");
        cut.Find("textarea").GetAttribute("aria-invalid").Should().Be("false");

        cut.Find("textarea").Blur();

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-invalid");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The Notes field is required.");
        cut.Find("textarea").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find("textarea").GetAttribute("aria-errormessage").Should().EndWith("-error");
    }

    [Fact]
    public void Inherits_Form_Appearance_Density_And_State() {
        var model = new TestModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Appearance, NTFormAppearance.Filled)
            .Add(p => p.Density, NTFormDensity.Dense)
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTTextArea>(0);
                builder.AddAttribute(1, nameof(NTTextArea.Value), model.Notes);
                builder.AddAttribute(2, nameof(NTTextArea.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Notes = value));
                builder.AddAttribute(3, nameof(NTTextArea.ValueExpression), (Expression<Func<string?>>)(() => model.Notes));
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-input").GetAttribute("class")!;
        rootClass.Should().Contain("nt-input-filled");
        rootClass.Should().Contain("nt-input-dense");
        rootClass.Should().Contain("nt-input-disabled");
        rootClass.Should().Contain("nt-textarea");
        cut.Find("textarea").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Prefix_And_Suffix_Are_Described_By_TextArea() {
        var cut = RenderTextArea(configure: parameters => parameters
            .Add(p => p.PrefixText, "Note")
            .Add(p => p.SuffixText, "internal"));

        var describedBy = cut.Find("textarea").GetAttribute("aria-describedby")!;
        describedBy.Should().Contain(cut.Find(".nt-input-prefix").GetAttribute("id"));
        describedBy.Should().Contain(cut.Find(".nt-input-suffix").GetAttribute("id"));
    }

    private IRenderedComponent<NTTextArea> RenderTextArea(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTTextArea>>? configure = null) {
        model ??= new TestModel();
        return Render<NTTextArea>(parameters => {
            parameters
                .Add(p => p.Value, model.Notes)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Notes = value))
                .Add(p => p.ValueExpression, (Expression<Func<string?>>)(() => model.Notes));
            configure?.Invoke(parameters);
        });
    }

    private static void AssertCharacterCounter(IRenderedComponent<NTTextArea> cut, string expectedText, string expectedMaxLength) {
        var counter = cut.Find(".nt-input-counter");
        counter.TextContent.Should().Be(expectedText);
        counter.GetAttribute("aria-label").Should().Be($"Character count {expectedText}");

        var textarea = cut.Find("textarea");
        textarea.GetAttribute("aria-describedby").Should().Contain(counter.GetAttribute("id"));
        textarea.GetAttribute("maxlength").Should().Be(expectedMaxLength);
        textarea.GetAttribute("oninput").Should().Be("window.NTComponents?.updateInputCounter?.(this)");
    }

    private sealed class TestTextArea : NTTextArea {
        public Task InvokeOnAfterRenderAsync(bool firstRender) => base.OnAfterRenderAsync(firstRender);

        public void ClearIsolatedJsModule() =>
            typeof(NTTextArea)
                .GetProperty(nameof(IsolatedJsModule))!
                .SetValue(this, null);
    }
}
