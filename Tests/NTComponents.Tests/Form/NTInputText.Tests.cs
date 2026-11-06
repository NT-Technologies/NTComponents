using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTInputText_Tests : BunitContext {

    private sealed class RequiredModel {
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string? Name { get; set; }
    }

    private sealed class TestModel {
        public string? Name { get; set; }
    }

    [Fact]
    public void Renders_Stable_Label_And_Input_Association() {
        var model = new TestModel();

        var cut = RenderInput(model, parameters => parameters
            .Add(p => p.ElementId, "name-input")
            .Add(p => p.Label, "Name")
            .Add(p => p.SupportingText, "Use your legal name"));

        var input = cut.Find("input");
        var label = cut.Find("label.nt-input-container");

        input.GetAttribute("id").Should().Be("name-input");
        label.GetAttribute("for").Should().Be("name-input");
        input.GetAttribute("aria-describedby").Should().Contain("name-input-supporting");
        cut.Find("#name-input-supporting").TextContent.Should().Be("Use your legal name");
    }

    [Fact]
    public void Generated_Input_Id_Is_Deterministic_For_Field() {
        var model = new TestModel();

        using var first = RenderInput(model, parameters => parameters
            .Add(p => p.Label, "Name")
            .Add(p => p.SupportingText, "Use your legal name"));
        var firstInputId = first.Find("input").GetAttribute("id");
        var firstLabelFor = first.Find("label.nt-input-container").GetAttribute("for");
        var firstSupportingTextId = first.Find(".nt-input-supporting").GetAttribute("id");

        firstInputId.Should().NotBeNullOrWhiteSpace();
        firstInputId.Should().StartWith("nt-input-model-name-");
        firstLabelFor.Should().Be(firstInputId);
        firstSupportingTextId.Should().Be($"{firstInputId}-supporting");

        using var second = RenderInput(model, parameters => parameters
            .Add(p => p.Label, "Name")
            .Add(p => p.SupportingText, "Use your legal name"));

        second.Find("input").GetAttribute("id").Should().Be(firstInputId);
        second.Find("label.nt-input-container").GetAttribute("for").Should().Be(firstInputId);
        second.Find(".nt-input-supporting").GetAttribute("id").Should().Be(firstSupportingTextId);
    }

    [Fact]
    public void Explicit_ErrorText_Wins_And_Uses_Error_Accessibility() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.Label, "Name")
            .Add(p => p.SupportingText, "Helper")
            .Add(p => p.ErrorText, "Name is required"));

        var input = cut.Find("input");
        var root = cut.Find(".nt-input");

        root.ClassList.Should().NotContain("nt-invalid");
        root.ClassList.Should().NotContain("nt-modified");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("Name is required");
        cut.FindAll(".nt-input-supporting:not(.nt-input-error-text)").Should().BeEmpty();
        input.GetAttribute("aria-invalid").Should().Be("true");
        input.GetAttribute("aria-errormessage").Should().EndWith("-error");
    }

    [Fact]
    public void Character_Counter_Renders_From_MaxLength() {
        var model = new TestModel { Name = "abc" };

        var cut = RenderInput(model, parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["maxlength"] = "8", ["data-field"] = "name" }));

        AssertCharacterCounter(cut, expectedText: "3/8", expectedMaxLength: "8", expectedDataField: "name");
    }

    [Fact]
    public void Character_Counter_Renders_For_Interactive_Renderer() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var model = new TestModel { Name = "abcdef" };

        var cut = RenderInput(model, parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["maxlength"] = "10", ["data-field"] = "interactive-name" }));

        AssertCharacterCounter(cut, expectedText: "6/10", expectedMaxLength: "10", expectedDataField: "interactive-name");
    }

    [Fact]
    public void Character_Counter_Renders_For_Static_Ssr_Renderer() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel { Name = "abcd" };

        var cut = RenderInput(model, parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["maxlength"] = "12", ["data-field"] = "ssr-name" }));

        AssertCharacterCounter(cut, expectedText: "4/12", expectedMaxLength: "12", expectedDataField: "ssr-name");
    }

    [Fact]
    public void Character_Counter_Script_Is_Not_Rendered_By_Component() {
        var cut = RenderInput();

        cut.Find("input").HasAttribute("oninput").Should().BeFalse();
        cut.FindAll("script[data-nt-input-counter-enhancer]").Should().BeEmpty();
    }

    [Fact]
    public void Color_Overrides_Emit_Component_Css_Variables() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.ActiveIndicatorColor, NTColor.Secondary)
            .Add(p => p.BackgroundColor, NTColor.SurfaceContainerHigh)
            .Add(p => p.CaretColor, NTColor.Tertiary)
            .Add(p => p.DisabledContainerColor, NTColor.SurfaceContainerLow)
            .Add(p => p.DisabledContentColor, NTColor.OnSurfaceVariant)
            .Add(p => p.DisabledOutlineColor, NTColor.OutlineVariant)
            .Add(p => p.ErrorCaretColor, NTColor.Warning)
            .Add(p => p.ErrorColor, NTColor.Error)
            .Add(p => p.FocusColor, NTColor.Primary)
            .Add(p => p.HoverActiveIndicatorColor, NTColor.OnSurface)
            .Add(p => p.HoverOutlineColor, NTColor.OnSurface)
            .Add(p => p.IconColor, NTColor.Tertiary)
            .Add(p => p.LabelColor, NTColor.Secondary)
            .Add(p => p.OutlineColor, NTColor.Outline)
            .Add(p => p.PlaceholderColor, NTColor.OnSurfaceVariant)
            .Add(p => p.PrefixSuffixColor, NTColor.Tertiary)
            .Add(p => p.StateLayerColor, NTColor.OnSurface)
            .Add(p => p.SupportingTextColor, NTColor.Secondary)
            .Add(p => p.TextColor, NTColor.OnSurface));

        var style = cut.Find(".nt-input").GetAttribute("style");

        style.Should().Contain("--nt-input-active-indicator-color:var(--nt-color-secondary);");
        style.Should().Contain("--nt-input-container-color:var(--nt-color-surface-container-high);");
        style.Should().Contain("--nt-input-caret-color:var(--nt-color-tertiary);");
        style.Should().Contain("--nt-input-disabled-container-color:var(--nt-color-surface-container-low);");
        style.Should().Contain("--nt-input-disabled-content-color:var(--nt-color-on-surface-variant);");
        style.Should().Contain("--nt-input-disabled-outline-color:var(--nt-color-outline-variant);");
        style.Should().Contain("--nt-input-error-caret-color:var(--nt-color-warning);");
        style.Should().Contain("--nt-input-error-color:var(--nt-color-error);");
        style.Should().Contain("--nt-input-focus-color:var(--nt-color-primary);");
        style.Should().Contain("--nt-input-hover-active-indicator-color:var(--nt-color-on-surface);");
        style.Should().Contain("--nt-input-hover-outline-color:var(--nt-color-on-surface);");
        style.Should().Contain("--nt-input-icon-color:var(--nt-color-tertiary);");
        style.Should().Contain("--nt-input-label-color:var(--nt-color-secondary);");
        style.Should().Contain("--nt-input-outline-color:var(--nt-color-outline);");
        style.Should().Contain("--nt-input-placeholder-color:var(--nt-color-on-surface-variant);");
        style.Should().Contain("--nt-input-prefix-suffix-color:var(--nt-color-tertiary);");
        style.Should().Contain("--nt-input-state-layer-color:var(--nt-color-on-surface);");
        style.Should().Contain("--nt-input-supporting-text-color:var(--nt-color-secondary);");
        style.Should().Contain("--nt-input-text-color:var(--nt-color-on-surface);");
    }

    [Fact]
    public void Required_Supporting_Text_Comes_From_NTForm() {
        var model = new RequiredModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ShowRequiredSupportingText, true)
            .Add(p => p.RequiredSupportingText, "Required for setup")
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTInputText>(0);
                builder.AddAttribute(1, nameof(NTInputText.Value), model.Name);
                builder.AddAttribute(2, nameof(NTInputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Name = value));
                builder.AddAttribute(3, nameof(NTInputText.ValueExpression), (Expression<Func<string?>>)(() => model.Name));
                builder.AddAttribute(4, nameof(NTInputText.AdditionalAttributes), new Dictionary<string, object> {
                    ["required"] = true,
                    ["minlength"] = "2",
                    ["maxlength"] = "10"
                });
                builder.CloseComponent();
            }));

        cut.Find(".nt-input-supporting").TextContent.Should().Be("Required for setup");
        cut.Find("input").HasAttribute("required").Should().BeTrue();
        cut.Find("input").GetAttribute("minlength").Should().Be("2");
        cut.Find("input").GetAttribute("maxlength").Should().Be("10");
    }

    [Fact]
    public void Blur_Validates_Required_Field_And_Renders_Error_State() {
        var model = new RequiredModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTInputText>(1);
                builder.AddAttribute(2, nameof(NTInputText.Value), model.Name);
                builder.AddAttribute(3, nameof(NTInputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Name = value));
                builder.AddAttribute(4, nameof(NTInputText.ValueExpression), (Expression<Func<string?>>)(() => model.Name));
                builder.CloseComponent();
            }));

        cut.Find(".nt-input").ClassList.Should().NotContain("nt-invalid");
        cut.Find(".nt-input").ClassList.Should().NotContain("nt-modified");
        cut.Find("input").GetAttribute("aria-invalid").Should().Be("false");

        cut.Find("input").Blur();

        cut.Find(".nt-input").ClassList.Should().Contain("nt-invalid");
        cut.Find(".nt-input").ClassList.Should().Contain("nt-modified");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("The Name field is required.");
        cut.Find("input").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find("input").GetAttribute("aria-errormessage").Should().EndWith("-error");
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
                builder.OpenComponent<NTInputText>(0);
                builder.AddAttribute(1, nameof(NTInputText.Value), model.Name);
                builder.AddAttribute(2, nameof(NTInputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Name = value));
                builder.AddAttribute(3, nameof(NTInputText.ValueExpression), (Expression<Func<string?>>)(() => model.Name));
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-input").GetAttribute("class")!;
        rootClass.Should().Contain("nt-input-filled");
        rootClass.Should().Contain("nt-input-dense");
        rootClass.Should().Contain("nt-input-disabled");
        cut.Find("input").HasAttribute("disabled").Should().BeTrue();
        cut.FindAll(".nt-input-outline").Should().BeEmpty();
        cut.Find(".nt-input-active-indicator").Should().NotBeNull();
    }

    [Fact]
    public void Field_Overrides_Form_Values() {
        var model = new TestModel();

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Appearance, NTFormAppearance.Filled)
            .Add(p => p.Density, NTFormDensity.Dense)
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTInputText>(0);
                builder.AddAttribute(1, nameof(NTInputText.Value), model.Name);
                builder.AddAttribute(2, nameof(NTInputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Name = value));
                builder.AddAttribute(3, nameof(NTInputText.ValueExpression), (Expression<Func<string?>>)(() => model.Name));
                builder.AddAttribute(4, nameof(NTInputText.Appearance), NTFormAppearance.Outlined);
                builder.AddAttribute(5, nameof(NTInputText.Density), NTFormDensity.Comfortable);
                builder.AddAttribute(6, nameof(NTInputText.Disabled), false);
                builder.CloseComponent();
            }));

        var rootClass = cut.Find(".nt-input").GetAttribute("class")!;
        rootClass.Should().Contain("nt-input-outlined");
        rootClass.Should().Contain("nt-input-comfortable");
        rootClass.Should().NotContain("nt-input-disabled");
        cut.Find("input").HasAttribute("disabled").Should().BeFalse();
        cut.Find(".nt-input-outline").Should().NotBeNull();
        cut.FindAll(".nt-input-active-indicator").Should().BeEmpty();
    }

    [Theory]
    [InlineData(TextInputType.Text, "text")]
    [InlineData(TextInputType.Email, "email")]
    [InlineData(TextInputType.Password, "password")]
    [InlineData(TextInputType.Search, "search")]
    [InlineData(TextInputType.Tel, "tel")]
    [InlineData(TextInputType.Url, "url")]
    public void InputType_Maps_To_Native_Input_Type(TextInputType inputType, string expectedType) {
        var cut = RenderInput(configure: parameters => parameters.Add(p => p.InputType, inputType));

        cut.Find("input").GetAttribute("type").Should().Be(expectedType);
    }

    [Fact]
    public void Tel_InputType_Adds_Default_Phone_Mask() {
        var model = new TestModel { Name = "1234567890" };

        var cut = RenderInput(model, parameters => parameters.Add(p => p.InputType, TextInputType.Tel));
        var input = cut.Find("input[type=tel]");

        input.GetAttribute("value").Should().Be("(123) 456-7890");
        input.GetAttribute("phoneMask").Should().Be("(###) ###-####");
        input.GetAttribute("oninput").Should().Be("window.NTComponents?.applyPhoneMaskInput?.(this)");
        input.HasAttribute("onkeydown").Should().BeFalse();
        input.HasAttribute("onkeyup").Should().BeFalse();
        input.HasAttribute("name").Should().BeFalse();
        cut.Find("input[type=hidden][name]").GetAttribute("value").Should().Be("1234567890");
    }

    [Fact]
    public void Tel_InputType_Uses_Custom_Phone_Mask() {
        var model = new TestModel { Name = "441234567890" };

        var cut = RenderInput(model, parameters => parameters
            .Add(p => p.InputType, TextInputType.Tel)
            .Add(p => p.PhoneMask, "+## #### ######"));
        var input = cut.Find("input[type=tel]");

        input.GetAttribute("value").Should().Be("+44 1234 567890");
        input.GetAttribute("phoneMask").Should().Be("+## #### ######");
        cut.Find("input[type=hidden][name]").GetAttribute("value").Should().Be("44 1234567890");
    }

    [Fact]
    public void Non_Tel_InputType_Does_Not_Add_Phone_Mask() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.InputType, TextInputType.Text)
            .Add(p => p.PhoneMask, "(###) ###-####"));
        var input = cut.Find("input[type=text]");

        input.HasAttribute("phoneMask").Should().BeFalse();
        input.HasAttribute("oninput").Should().BeFalse();
    }

    [Fact]
    public void Tel_InputType_Composes_Phone_Mask_And_Counter_OnInput_Handlers() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.InputType, TextInputType.Tel)
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> { ["maxlength"] = "14" }));

        cut.Find("input[type=tel]").GetAttribute("oninput")
            .Should().Be("window.NTComponents?.applyPhoneMaskInput?.(this); window.NTComponents?.updateInputCounter?.(this)");
    }

    [Fact]
    public void Tel_InputType_Change_Stores_Normalized_Value() {
        var model = new TestModel();
        var cut = RenderInput(model, parameters => parameters.Add(p => p.InputType, TextInputType.Tel));

        cut.Find("input[type=tel]").Change("1234567890");

        model.Name.Should().Be("1234567890");
    }

    [Fact]
    public void Tel_InputType_Change_With_Country_Mask_Stores_Country_Code_And_Number() {
        var model = new TestModel();
        var cut = RenderInput(model, parameters => parameters
            .Add(p => p.InputType, TextInputType.Tel)
            .Add(p => p.PhoneMask, "+## #### ######"));

        cut.Find("input[type=tel]").Change("+44 1234 567890");

        model.Name.Should().Be("44 1234567890");
    }

    [Fact]
    public void Prefix_And_Suffix_Are_Described_By_Input() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.PrefixText, "$")
            .Add(p => p.SuffixText, "USD"));

        var describedBy = cut.Find("input").GetAttribute("aria-describedby")!;
        describedBy.Should().Contain(cut.Find(".nt-input-prefix").GetAttribute("id"));
        describedBy.Should().Contain(cut.Find(".nt-input-suffix").GetAttribute("id"));
    }

    [Fact]
    public void Outlined_Notch_Uses_Fixed_Label_Position_For_Start_Adornments() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.Label, "Amount")
            .Add(p => p.PrefixText, "$")
            .Add(p => p.LeadingIcon, MaterialIcon.Person));

        cut.Find(".nt-input-outline").Should().NotBeNull();
        cut.FindAll(".nt-input-outline-leading-spacer").Should().BeEmpty();
        cut.FindAll(".nt-input-outline-prefix-spacer").Should().BeEmpty();
        cut.Find(".nt-input-outline-label").TextContent.Should().Be("Amount");
        cut.Find(".nt-input-prefix").TextContent.Should().Be("$");
        cut.Find(".nt-input-leading").Should().NotBeNull();
    }

    [Fact]
    public void Filled_Field_Does_Not_Render_Outlined_Notch_Dom() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.Appearance, NTFormAppearance.Filled)
            .Add(p => p.Label, "Name")
            .Add(p => p.PrefixText, "$")
            .Add(p => p.LeadingIcon, MaterialIcon.Person));

        cut.FindAll(".nt-input-outline").Should().BeEmpty();
        cut.FindAll(".nt-input-outline-start").Should().BeEmpty();
        cut.FindAll(".nt-input-outline-notch").Should().BeEmpty();
        cut.FindAll(".nt-input-outline-label").Should().BeEmpty();
        cut.FindAll(".nt-input-outline-prefix-spacer").Should().BeEmpty();
        cut.Find(".nt-input-active-indicator").Should().NotBeNull();
    }

    [Fact]
    public void Explicit_Name_Attribute_Is_Used_Without_Duplicate_Pass_Through() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
                ["name"] = "Customer.Name",
                ["class"] = "user-class",
                ["data-field"] = "name"
            }));

        var input = cut.Find("input");
        input.GetAttribute("name").Should().Be("Customer.Name");
        input.GetAttribute("class").Should().Contain("nt-input-control");
        input.GetAttribute("class").Should().Contain("user-class");
        input.GetAttribute("data-field").Should().Be("name");
    }

    [Fact]
    public void Blur_After_Change_Does_Not_Notify_Field_Changed_Twice() {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var fieldChangedCount = 0;
        editContext.OnFieldChanged += (_, _) => fieldChangedCount++;

        var cut = Render<NTForm>(parameters => parameters
            .Add(p => p.EditContext, editContext)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTInputText>(0);
                builder.AddAttribute(1, nameof(NTInputText.Value), model.Name);
                builder.AddAttribute(2, nameof(NTInputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Name = value));
                builder.AddAttribute(3, nameof(NTInputText.ValueExpression), (Expression<Func<string?>>)(() => model.Name));
                builder.CloseComponent();
            }));

        cut.Find("input").Change("Ada");
        cut.Find("input").Blur();

        fieldChangedCount.Should().Be(1);
    }

    [Fact]
    public void Blur_Without_A_Change_Does_Not_Fire_BindAfter() {
        var bindAfterCount = 0;
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.BindAfter, EventCallback.Factory.Create<string?>(this, _ => bindAfterCount++)));

        cut.Find("input").Blur();

        bindAfterCount.Should().Be(0);
    }

    [Fact]
    public void Change_Fires_BindAfter_Once_At_The_Next_Blur() {
        string? bindAfterValue = null;
        var bindAfterCount = 0;
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.BindAfter, EventCallback.Factory.Create<string?>(this, value => {
                bindAfterCount++;
                bindAfterValue = value;
            })));

        cut.Find("input").Change("Ada");
        bindAfterCount.Should().Be(0);
        cut.Find("input").Blur();
        cut.Find("input").Blur();

        bindAfterCount.Should().Be(1);
        bindAfterValue.Should().Be("Ada");
    }

    [Fact]
    public void Placeholder_Adds_Placeholder_State_Class() {
        var cut = RenderInput(configure: parameters => parameters
            .Add(p => p.Label, "Name")
            .Add(p => p.Placeholder, "Enter name"));

        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-input-has-placeholder");
        cut.Find("input").GetAttribute("placeholder").Should().Be("Enter name");
    }

    [Fact]
    public void Synthetic_Placeholder_Does_Not_Add_Placeholder_State_Class() {
        var cut = RenderInput(configure: parameters => parameters.Add(p => p.Label, "Name"));

        cut.Find(".nt-input").GetAttribute("class").Should().NotContain("nt-input-has-placeholder");
        cut.Find("input").GetAttribute("placeholder").Should().Be(" ");
    }

    [Fact]
    public void InputTypeAttribute_Is_Typed_Enum() {
        new ExposedInputBase().ExposedInputTypeAttribute.Should().Be(InputType.Text);
    }

    private IRenderedComponent<NTInputText> RenderInput(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputText>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputText>(parameters => {
            parameters
                .Add(p => p.Value, model.Name)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Name = value))
                .Add(p => p.ValueExpression, (Expression<Func<string?>>)(() => model.Name));
            configure?.Invoke(parameters);
        });
    }

    private static void AssertCharacterCounter(IRenderedComponent<NTInputText> cut, string expectedText, string expectedMaxLength, string expectedDataField) {
        var counter = cut.Find(".nt-input-counter");
        counter.TextContent.Should().Be(expectedText);
        counter.GetAttribute("aria-label").Should().Be($"Character count {expectedText}");

        var input = cut.Find("input");
        input.GetAttribute("aria-describedby").Should().Contain(counter.GetAttribute("id"));
        input.GetAttribute("maxlength").Should().Be(expectedMaxLength);
        input.GetAttribute("data-field").Should().Be(expectedDataField);
        input.GetAttribute("oninput").Should().Be("window.NTComponents?.updateInputCounter?.(this)");
        cut.FindAll("script[data-nt-input-counter-enhancer]").Should().BeEmpty();
    }

    private sealed class ExposedInputBase : NTInputBase<string?> {
        public InputType ExposedInputTypeAttribute => InputTypeAttribute;

        protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage) {
            result = value;
            validationErrorMessage = string.Empty;
            return true;
        }
    }
}
