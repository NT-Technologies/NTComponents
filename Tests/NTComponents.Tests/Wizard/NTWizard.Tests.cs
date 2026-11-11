using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel.DataAnnotations;

namespace NTComponents.Tests.Wizard;

public class NTWizard_Tests : BunitContext {

    public NTWizard_Tests() {
        TestingUtility.TestingUtility.SetupRippleEffectModule(this);
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void AdditionalAttributes_Are_Applied() {
        var attrs = new Dictionary<string, object> {
            { "class", "custom-wizard" },
            { "data-test", "wizard" }
        };

        var cut = Render<NTWizard>(p => p
            .Add(c => c.AdditionalAttributes, attrs)
            .AddChildContent("Content"));

        var wizard = cut.Find("div.nt-wizard");
        var cls = wizard.GetAttribute("class")!;
        cls.Should().Contain("custom-wizard");
        cls.Should().Contain("nt-wizard");
        wizard.GetAttribute("data-test").Should().Be("wizard");
    }

    [Fact]
    public void Multiple_Steps_Render_All_Step_Indicators() {
        var cut = RenderWizardWithThreeSteps();

        cut.FindAll("li.nt-wizard-step-indicator").Should().HaveCount(3);
    }

    [Fact]
    public void PushNavigationToBottom_False_Removes_Bottom_Placement_Class() {
        var cut = RenderWizardWithThreeSteps(wizard => wizard.Add(w => w.PushNavigationToBottom, false));

        cut.Find("div.nt-wizard").GetAttribute("class")!.Should().NotContain("nt-wizard-buttons-on-bottom");
    }

    [Fact]
    public async Task Next_Button_Advances_Step() {
        var cut = RenderWizardWithThreeSteps();

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("completed-step");
        indicators[1].GetAttribute("class")!.Should().Contain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 2 content");
    }

    [Fact]
    public async Task Next_Button_Skips_Disabled_Steps() {
        var cut = RenderWizardWithThreeSteps(step2Disabled: true);

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 3 content");
        cut.FindAll("li.nt-wizard-step-indicator")[1].GetAttribute("class")!.Should().Contain("disabled-step");
    }

    [Fact]
    public async Task Optional_Current_Step_Can_Be_Skipped_Without_Being_Completed() {
        var skippedIndex = -1;
        var cut = RenderWizardWithThreeSteps(
            step1Optional: true,
            configureWizard: wizard => wizard.Add(w => w.OnStepSkipped, EventCallback.Factory.Create<int>(this, index => skippedIndex = index)));

        await FindButton(cut, "Skip").ClickAsync(new MouseEventArgs());

        skippedIndex.Should().Be(0);
        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("skipped-step");
        indicators[0].GetAttribute("class")!.Should().NotContain("completed-step");
        indicators[1].GetAttribute("class")!.Should().Contain("current-step");
    }

    [Fact]
    public async Task Free_Navigation_Allows_Unvisited_Enabled_Step_Header_Click() {
        var cut = RenderWizardWithThreeSteps(configureWizard: wizard => wizard.Add(w => w.NavigationMode, NTWizardNavigationMode.Free));

        cut.FindAll("li.nt-wizard-step-indicator")[2].GetAttribute("class")!.Should().Contain("available-step");

        await FindStepButton(cut, "Step 3").ClickAsync(new MouseEventArgs());

        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 3 content");
    }

    [Fact]
    public async Task Free_Navigation_Validates_Current_Form_Before_Jumping_To_Future_Step() {
        var invalidSubmitCalled = false;
        var model = new WizardRequiredModel();
        var cut = Render<NTWizard>(p => p
            .Add(w => w.NavigationMode, NTWizardNavigationMode.Free)
            .Add(w => w.InvalidFormButtonBehavior, NTWizardInvalidFormButtonBehavior.GrayOutOnly)
            .AddChildContent(builder => {
                builder.OpenComponent<NTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(NTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(NTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(NTWizardFormStep.OnInvalidSubmitCallback), EventCallback.Factory.Create<object>(this, _ => invalidSubmitCalled = true));
                builder.AddComponentParameter(40, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")));
                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(50);
                builder.AddComponentParameter(60, nameof(NTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(70, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(80);
                builder.AddComponentParameter(90, nameof(NTWizardStep.Title), "Step 3");
                builder.AddComponentParameter(100, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 3 content")));
                builder.CloseComponent();
            }));

        await FindStepButton(cut, "Step 3").ClickAsync(new MouseEventArgs());

        invalidSubmitCalled.Should().BeTrue();
        cut.FindAll("li.nt-wizard-step-indicator")[0].GetAttribute("class")!.Should().Contain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Form content");
    }

    [Fact]
    public async Task Free_Navigation_ValidateOnNavigation_False_Allows_Jumping_To_Future_Step() {
        var invalidSubmitCalled = false;
        var model = new WizardRequiredModel();
        var cut = Render<NTWizard>(p => p
            .Add(w => w.NavigationMode, NTWizardNavigationMode.Free)
            .Add(w => w.InvalidFormButtonBehavior, NTWizardInvalidFormButtonBehavior.DisableButtons)
            .AddChildContent(builder => {
                builder.OpenComponent<NTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(NTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(NTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(NTWizardFormStep.ValidateOnNavigation), false);
                builder.AddComponentParameter(40, nameof(NTWizardFormStep.OnInvalidSubmitCallback), EventCallback.Factory.Create<object>(this, _ => invalidSubmitCalled = true));
                builder.AddComponentParameter(50, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")));
                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(60);
                builder.AddComponentParameter(70, nameof(NTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(80, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(90);
                builder.AddComponentParameter(100, nameof(NTWizardStep.Title), "Step 3");
                builder.AddComponentParameter(110, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 3 content")));
                builder.CloseComponent();
            }));

        await FindStepButton(cut, "Step 3").ClickAsync(new MouseEventArgs());

        invalidSubmitCalled.Should().BeFalse();
        cut.FindAll("li.nt-wizard-step-indicator")[2].GetAttribute("class")!.Should().Contain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 3 content");
    }

    [Fact]
    public async Task Step_Validation_Callback_Blocks_Next_When_Invalid() {
        var validatorCalled = false;
        var cut = RenderWizardWithThreeSteps(step1ValidateAsync: () => {
            validatorCalled = true;
            return Task.FromResult(false);
        });

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        validatorCalled.Should().BeTrue();
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 1 content");
        cut.FindAll("li.nt-wizard-step-indicator")[0].GetAttribute("class")!.Should().Contain("invalid-step");
    }

    [Fact]
    public async Task Step_Indicators_Mark_Current_Next_Completed_And_Aria_Current() {
        var cut = RenderWizardWithThreeSteps();

        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        var stepButtons = cut.FindAll("button.nt-wizard-step-button");
        indicators[0].GetAttribute("class")!.Should().Contain("current-step");
        indicators[0].GetAttribute("data-state").Should().Be("current");
        stepButtons[0].GetAttribute("aria-current").Should().Be("step");
        indicators[1].GetAttribute("class")!.Should().Contain("next-step");
        stepButtons[1].HasAttribute("aria-current").Should().BeFalse();
        indicators[2].GetAttribute("class")!.Should().NotContain("completed-step");

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        indicators = cut.FindAll("li.nt-wizard-step-indicator");
        stepButtons = cut.FindAll("button.nt-wizard-step-button");
        indicators[0].GetAttribute("class")!.Should().Contain("completed-step");
        indicators[0].GetAttribute("data-state").Should().Be("completed");
        stepButtons[0].HasAttribute("aria-current").Should().BeFalse();
        indicators[1].GetAttribute("class")!.Should().Contain("current-step");
        indicators[1].GetAttribute("data-state").Should().Be("current");
        stepButtons[1].GetAttribute("aria-current").Should().Be("step");
        indicators[2].GetAttribute("class")!.Should().Contain("next-step");
    }

    [Fact]
    public async Task Step_Click_Allows_Immediate_Next_And_Fires_Next_Callback() {
        var nextIndex = -1;
        var cut = RenderWizardWithThreeSteps(p => p.Add(w => w.OnNextButtonClicked, EventCallback.Factory.Create<int>(this, i => nextIndex = i)));

        await cut.FindAll("button.nt-wizard-step-button")[1].ClickAsync(new MouseEventArgs());

        nextIndex.Should().Be(1);
        cut.FindAll("li.nt-wizard-step-indicator")[1].GetAttribute("class")!.Should().Contain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 2 content");
    }

    [Fact]
    public async Task Step_Click_Does_Not_Allow_Unvisited_Non_Immediate_Next() {
        var cut = RenderWizardWithThreeSteps();

        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[2].GetAttribute("class")!.Should().NotContain("available-step");

        await cut.FindAll("button.nt-wizard-step-button")[2].ClickAsync(new MouseEventArgs());

        indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("current-step");
        indicators[2].GetAttribute("class")!.Should().NotContain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 1 content");
    }

    [Fact]
    public async Task Step_Click_Allows_Navigation_To_Visited_Step() {
        var cut = RenderWizardWithThreeSteps();
        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        await cut.FindAll("button.nt-wizard-step-button")[0].ClickAsync(new MouseEventArgs());

        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("current-step");
        indicators[1].GetAttribute("class")!.Should().NotContain("completed-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 1 content");
    }

    [Fact]
    public void Configured_Step_States_Render_Optional_Skipped_Disabled_Invalid_And_Completed_Classes() {
        var cut = Render<NTWizard>(p => p.AddChildContent(builder => {
            AddStep(builder, "Current", "Current content");
            AddStep(builder, "Optional", "Optional content", optional: true);
            AddStep(builder, "Skipped", "Skipped content", skipped: true);
            AddStep(builder, "Disabled", "Disabled content", disabled: true);
            AddStep(builder, "Error", "Error content", hasError: true);
            AddStep(builder, "Completed", "Completed content", completed: true);
        }));

        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[1].GetAttribute("class")!.Should().Contain("optional-step");
        indicators[2].GetAttribute("class")!.Should().Contain("skipped-step");
        indicators[2].GetAttribute("data-state").Should().Be("skipped");
        indicators[3].GetAttribute("class")!.Should().Contain("disabled-step");
        indicators[3].GetAttribute("data-state").Should().Be("disabled");
        indicators[4].GetAttribute("class")!.Should().Contain("invalid-step");
        indicators[4].GetAttribute("data-state").Should().Be("invalid");
        indicators[5].GetAttribute("class")!.Should().Contain("completed-step");
        indicators[5].GetAttribute("data-state").Should().Be("completed");
    }

    [Fact]
    public async Task Optional_Step_Can_Be_Skipped_And_Marks_Skipped_State() {
        var skippedIndex = -1;
        var cut = Render<NTWizard>(p => p
            .Add(w => w.OnStepSkipped, EventCallback.Factory.Create<int>(this, i => skippedIndex = i))
            .AddChildContent(builder => {
                AddStep(builder, "Optional", "Optional content", optional: true);
                AddStep(builder, "Step 2", "Step 2 content");
            }));

        await FindSkipButton(cut).ClickAsync(new MouseEventArgs());

        skippedIndex.Should().Be(0);
        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("skipped-step");
        indicators[0].GetAttribute("data-state").Should().Be("skipped");
        indicators[1].GetAttribute("class")!.Should().Contain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 2 content");
    }

    [Fact]
    public async Task Optional_Form_Step_With_Required_Field_Skip_Marks_Skipped_And_Invalid() {
        var invalidSubmitCalled = false;
        var model = new WizardRequiredModel();
        var cut = Render<NTWizard>(p => p.AddChildContent(builder => {
            AddStep(builder, "Step 1", "Step 1 content");

            builder.OpenComponent<NTWizardFormStep>(10);
            builder.AddComponentParameter(20, nameof(NTWizardFormStep.Title), "Step 2");
            builder.AddComponentParameter(30, nameof(NTWizardFormStep.Optional), true);
            builder.AddComponentParameter(40, nameof(NTWizardFormStep.Model), model);
            builder.AddComponentParameter(50, nameof(NTWizardFormStep.OnInvalidSubmitCallback), EventCallback.Factory.Create<object>(this, _ => invalidSubmitCalled = true));
            builder.AddComponentParameter(60, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Step 2 form")));
            builder.CloseComponent();

            AddStep(builder, "Step 3", "Step 3 content");
        }));

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());
        await FindSkipButton(cut).ClickAsync(new MouseEventArgs());

        invalidSubmitCalled.Should().BeFalse();
        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[1].GetAttribute("class")!.Should().Contain("skipped-step");
        indicators[1].GetAttribute("class")!.Should().Contain("invalid-step");
        indicators[1].GetAttribute("data-state").Should().Be("invalid");
        cut.FindAll("button.nt-wizard-step-button")[1].GetAttribute("aria-invalid").Should().Be("true");
        indicators[2].GetAttribute("class")!.Should().Contain("current-step");
    }

    [Fact]
    public async Task Visited_Form_Step_State_Persists_When_Navigating_Forward_And_Back() {
        var step1 = new WizardRequiredModel { Name = "Ready" };
        var step3 = new WizardRequiredModel();
        var step5 = new WizardRequiredModel();
        var cut = Render<NTWizard>(p => p
            .Add(w => w.InvalidFormButtonBehavior, NTWizardInvalidFormButtonBehavior.DisableButtons)
            .AddChildContent(builder => {
                AddRequiredFormStep(builder, "Step 1", step1);
                AddStep(builder, "Step 2", "Step 2 content");
                AddRequiredFormStep(builder, "Step 3", step3, validateOnNavigation: false);
                AddStep(builder, "Step 4", "Step 4 content");
                AddRequiredFormStep(builder, "Step 5", step5);
            }));

        await AssertStepStateAsync(cut, current: 0, completed: [], invalid: []);

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());
        await AssertStepStateAsync(cut, current: 1, completed: [0], invalid: []);

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());
        await AssertStepStateAsync(cut, current: 2, completed: [0, 1], invalid: [2]);

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());
        await AssertStepStateAsync(cut, current: 3, completed: [0, 1], invalid: [2]);

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());
        await AssertStepStateAsync(cut, current: 4, completed: [0, 1, 3], invalid: [2, 4]);

        await FindPreviousButton(cut).ClickAsync(new MouseEventArgs());
        await AssertStepStateAsync(cut, current: 3, completed: [0, 1], invalid: [2, 4]);

        await FindPreviousButton(cut).ClickAsync(new MouseEventArgs());
        await AssertStepStateAsync(cut, current: 2, completed: [0, 1, 3], invalid: [2, 4]);

        await FindPreviousButton(cut).ClickAsync(new MouseEventArgs());
        await AssertStepStateAsync(cut, current: 1, completed: [0, 3], invalid: [2, 4]);

        await FindPreviousButton(cut).ClickAsync(new MouseEventArgs());
        await AssertStepStateAsync(cut, current: 0, completed: [1, 3], invalid: [2, 4]);

        static async Task AssertStepStateAsync(IRenderedComponent<NTWizard> cut, int current, int[] completed, int[] invalid) {
            await cut.InvokeAsync(() => Task.CompletedTask);
            cut.WaitForAssertion(() => {
                var indicators = cut.FindAll("li.nt-wizard-step-indicator");
                indicators.Should().HaveCount(5);
                for (var i = 0; i < indicators.Count; i++) {
                    var classes = indicators[i].GetAttribute("class")!;
                    classes.Should().Contain("nt-wizard-step-indicator");
                    if (i == current) {
                        classes.Should().Contain("current-step");
                    }
                    if (i != current) {
                        classes.Should().NotContain("current-step");
                    }

                    if (completed.Contains(i)) {
                        classes.Should().Contain("completed-step");
                    }
                    else {
                        classes.Should().NotContain("completed-step");
                    }

                    if (invalid.Contains(i)) {
                        classes.Should().Contain("invalid-step");
                        cut.FindAll("button.nt-wizard-step-button")[i].GetAttribute("aria-invalid").Should().Be("true");
                    }
                    else {
                        classes.Should().NotContain("invalid-step");
                        cut.FindAll("button.nt-wizard-step-button")[i].GetAttribute("aria-invalid").Should().Be("false");
                    }
                }
            });
        }
    }

    [Fact]
    public async Task Optional_Step_Skip_Does_Not_Reset_When_Parent_Callback_Renders() {
        var cut = Render<WizardSkipCallbackHost>();

        await FindButton(cut.FindComponent<NTWizard>(), "Skip").ClickAsync(new MouseEventArgs());

        cut.Instance.SkippedIndex.Should().Be(0);
        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("skipped-step");
        indicators[0].GetAttribute("class")!.Should().NotContain("current-step");
        indicators[2].GetAttribute("class")!.Should().Contain("current-step");
        cut.Markup.Should().Contain("Skipped: 0");
    }

    [Fact]
    public async Task Disabled_Step_Is_Not_Enterable_And_Next_Skips_To_Next_Enabled_Step() {
        var cut = Render<NTWizard>(p => p.AddChildContent(builder => {
            AddStep(builder, "Step 1", "Step 1 content");
            AddStep(builder, "Step 2", "Step 2 content", disabled: true);
            AddStep(builder, "Step 3", "Step 3 content");
        }));

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("completed-step");
        indicators[1].GetAttribute("class")!.Should().Contain("disabled-step");
        indicators[1].GetAttribute("class")!.Should().NotContain("current-step");
        indicators[2].GetAttribute("class")!.Should().Contain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 3 content");
    }

    [Fact]
    public void FormStep_Uses_NTForm() {
        var model = new WizardRequiredModel { Name = "Ready" };

        var cut = Render<NTWizard>(p => p.AddChildContent<NTWizardFormStep>(step => step
            .Add(s => s.Title, "Form")
            .Add(s => s.Model, model)
            .Add(s => s.Appearance, NTFormAppearance.Filled)
            .Add(s => s.Density, NTFormDensity.Dense)
            .Add(s => s.ChildContent, (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")))));

        var form = cut.FindComponent<NTForm>().Instance;
        form.Appearance.Should().Be(NTFormAppearance.Filled);
        form.Density.Should().Be(NTFormDensity.Dense);
    }

    [Fact]
    public void DisableButtons_Mode_Starts_Disabled_For_Invalid_Required_Model() {
        var model = new WizardRequiredModel();
        var cut = RenderWizardWithFormStep(model, NTWizardInvalidFormButtonBehavior.DisableButtons);

        cut.WaitForAssertion(() => {
            FindNextButton(cut).HasAttribute("disabled").Should().BeTrue();
            cut.FindAll("li.nt-wizard-step-indicator")[1].GetAttribute("class")!.Should().NotContain("available-step");
        });
    }

    [Fact]
    public void GrayOutOnly_Mode_Starts_Gray_Not_Disabled_For_Invalid_Required_Model() {
        var model = new WizardRequiredModel();
        var cut = RenderWizardWithFormStep(model, NTWizardInvalidFormButtonBehavior.GrayOutOnly);

        cut.WaitForAssertion(() => {
            var nextButton = FindNextButton(cut);
            nextButton.HasAttribute("disabled").Should().BeFalse();
            nextButton.GetAttribute("class")!.Should().Contain("nt-disabled");
        });
    }

    [Fact]
    public async Task GrayOutOnly_Mode_Click_Still_Validates_And_Blocks_Advance() {
        var invalidSubmitCalled = false;
        var model = new WizardRequiredModel();
        var cut = Render<NTWizard>(p => p
            .Add(w => w.InvalidFormButtonBehavior, NTWizardInvalidFormButtonBehavior.GrayOutOnly)
            .AddChildContent(builder => {
                builder.OpenComponent<NTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(NTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(NTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(NTWizardFormStep.OnInvalidSubmitCallback), EventCallback.Factory.Create<object>(this, _ => invalidSubmitCalled = true));
                builder.AddComponentParameter(40, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")));
                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(50);
                builder.AddComponentParameter(60, nameof(NTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(70, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();
            }));

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        invalidSubmitCalled.Should().BeTrue();
        cut.FindAll("li.nt-wizard-step-indicator")[0].GetAttribute("class")!.Should().Contain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Form content");
    }

    [Fact]
    public async Task ValidateOnNavigation_False_Allows_Invalid_Form_To_Advance() {
        var invalidSubmitCalled = false;
        var model = new WizardRequiredModel();
        var cut = Render<NTWizard>(p => p
            .Add(w => w.InvalidFormButtonBehavior, NTWizardInvalidFormButtonBehavior.DisableButtons)
            .AddChildContent(builder => {
                builder.OpenComponent<NTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(NTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(NTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(NTWizardFormStep.ValidateOnNavigation), false);
                builder.AddComponentParameter(40, nameof(NTWizardFormStep.OnInvalidSubmitCallback), EventCallback.Factory.Create<object>(this, _ => invalidSubmitCalled = true));
                builder.AddComponentParameter(50, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")));
                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(60);
                builder.AddComponentParameter(70, nameof(NTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(80, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();
            }));

        cut.WaitForAssertion(() => FindNextButton(cut).HasAttribute("disabled").Should().BeFalse());
        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        invalidSubmitCalled.Should().BeFalse();
        cut.FindAll("li.nt-wizard-step-indicator")[1].GetAttribute("class")!.Should().Contain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 2 content");
    }

    [Fact]
    public async Task ValidateOnNavigation_False_Marks_Each_Visited_Invalid_Form_Step_Invalid() {
        var firstModel = new WizardRequiredModel();
        var secondModel = new WizardRequiredModel();
        var thirdModel = new WizardRequiredModel();
        var cut = Render<NTWizard>(p => p.AddChildContent(builder => {
            AddRequiredFormStep(builder, "Step 1", firstModel, validateOnNavigation: false);
            AddRequiredFormStep(builder, "Step 2", secondModel, validateOnNavigation: false);
            AddRequiredFormStep(builder, "Step 3", thirdModel, validateOnNavigation: false);
            AddStep(builder, "Review", "Review content");
        }));

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());
        await FindNextButton(cut).ClickAsync(new MouseEventArgs());
        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => {
            var indicators = cut.FindAll("li.nt-wizard-step-indicator");
            indicators[0].GetAttribute("class")!.Should().Contain("invalid-step").And.NotContain("completed-step");
            indicators[1].GetAttribute("class")!.Should().Contain("invalid-step").And.NotContain("completed-step");
            indicators[2].GetAttribute("class")!.Should().Contain("invalid-step").And.NotContain("completed-step");
            indicators[3].GetAttribute("class")!.Should().Contain("current-step");
        });
    }

    [Fact]
    public async Task ValidateOnNavigation_False_Submit_Still_Validates_Current_Form() {
        var invalidSubmitCalled = false;
        var submitCalled = false;
        var model = new WizardRequiredModel();
        var cut = Render<NTWizard>(p => p
            .Add(w => w.InvalidFormButtonBehavior, NTWizardInvalidFormButtonBehavior.GrayOutOnly)
            .Add(w => w.OnSubmitCallback, EventCallback.Factory.Create(this, () => submitCalled = true))
            .AddChildContent(builder => {
                builder.OpenComponent<NTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(NTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(NTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(NTWizardFormStep.ValidateOnNavigation), false);
                builder.AddComponentParameter(40, nameof(NTWizardFormStep.OnInvalidSubmitCallback), EventCallback.Factory.Create<object>(this, _ => invalidSubmitCalled = true));
                builder.AddComponentParameter(50, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")));
                builder.CloseComponent();
            }));

        await cut.FindAll("button").Single(button => button.TextContent.Contains("Submit")).ClickAsync(new MouseEventArgs());

        invalidSubmitCalled.Should().BeTrue();
        submitCalled.Should().BeFalse();
    }

    [Fact]
    public void DisableButtons_Mode_Reevaluates_On_Field_Change_Without_Global_Validation() {
        var model = new WizardRequiredModel();
        var cut = RenderWizardWithFormStep(model, NTWizardInvalidFormButtonBehavior.DisableButtons);
        cut.WaitForAssertion(() => FindNextButton(cut).HasAttribute("disabled").Should().BeTrue());

        var editContext = cut.FindComponent<NTForm>().Instance.EditContext!;
        model.Name = "valid";
        editContext.NotifyFieldChanged(new FieldIdentifier(model, nameof(WizardRequiredModel.Name)));

        cut.WaitForAssertion(() => FindNextButton(cut).HasAttribute("disabled").Should().BeFalse());
    }

    [Fact]
    public void FormStep_Validates_On_Input_So_Next_Button_Updates_While_Typing() {
        var model = new WizardRequiredModel();
        var cut = RenderWizardWithFormStep(model, NTWizardInvalidFormButtonBehavior.DisableButtons, editContext => builder => {
            builder.OpenComponent<NTInputText>(0);
            builder.AddComponentParameter(10, nameof(NTInputText.Label), "Name");
            builder.AddComponentParameter(20, nameof(NTInputText.Value), model.Name);
            builder.AddComponentParameter(30, nameof(NTInputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Name = value ?? string.Empty));
            builder.AddComponentParameter(40, nameof(NTInputText.ValueExpression), (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Name));
            builder.CloseComponent();
        });
        cut.WaitForAssertion(() => FindNextButton(cut).HasAttribute("disabled").Should().BeTrue());

        cut.Find("input").Input("valid");

        cut.WaitForAssertion(() => {
            model.Name.Should().Be("valid");
            FindNextButton(cut).HasAttribute("disabled").Should().BeFalse();
        });
    }

    [Fact]
    public void FormStep_Live_Input_Validation_Keeps_Next_Disabled_For_NonRequired_Errors() {
        var model = new WizardMinLengthModel();
        var cut = RenderWizardWithMinLengthFormStep(model);
        cut.WaitForAssertion(() => FindNextButton(cut).HasAttribute("disabled").Should().BeTrue());

        cut.Find("input").Input("abc");

        cut.WaitForAssertion(() => {
            model.Name.Should().Be("abc");
            FindNextButton(cut).HasAttribute("disabled").Should().BeTrue();
        });

        cut.Find("input").Input("abcdef");

        cut.WaitForAssertion(() => {
            model.Name.Should().Be("abcdef");
            FindNextButton(cut).HasAttribute("disabled").Should().BeFalse();
        });
    }

    [Fact]
    public async Task Async_Valid_Form_Callback_Completes_Before_Advancing() {
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCompleted = false;
        var model = new WizardRequiredModel { Name = "Ready" };
        var cut = Render<NTWizard>(p => p.AddChildContent(builder => {
            builder.OpenComponent<NTWizardFormStep>(0);
            builder.AddComponentParameter(10, nameof(NTWizardFormStep.Title), "Form");
            builder.AddComponentParameter(20, nameof(NTWizardFormStep.Model), model);
            builder.AddComponentParameter(30, nameof(NTWizardFormStep.OnValidSubmitCallback), EventCallback.Factory.Create<object>(this, async _ => {
                callbackStarted.SetResult();
                await releaseCallback.Task;
                callbackCompleted = true;
            }));
            builder.AddComponentParameter(40, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, "Form content")));
            builder.CloseComponent();

            builder.OpenComponent<NTWizardStep>(50);
            builder.AddComponentParameter(60, nameof(NTWizardStep.Title), "Step 2");
            builder.AddComponentParameter(70, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
            builder.CloseComponent();
        }));

        var clickTask = FindNextButton(cut).ClickAsync(new MouseEventArgs());
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(1), Xunit.TestContext.Current.CancellationToken);

        callbackCompleted.Should().BeFalse();
        cut.FindAll("li.nt-wizard-step-indicator")[0].GetAttribute("class")!.Should().Contain("current-step");

        releaseCallback.SetResult();
        await clickTask;

        callbackCompleted.Should().BeTrue();
        cut.FindAll("li.nt-wizard-step-indicator")[1].GetAttribute("class")!.Should().Contain("current-step");
    }

    [Fact]
    public async Task Enter_Key_Does_Not_Advance_When_Next_Button_Disabled() {
        var cut = RenderWizardWithThreeSteps(p => p.Add(w => w.NextButtonDisabled, true));

        await cut.Find("div.nt-wizard-content").KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        var indicators = cut.FindAll("li.nt-wizard-step-indicator");
        indicators[0].GetAttribute("class")!.Should().Contain("current-step");
        indicators[1].GetAttribute("class")!.Should().NotContain("current-step");
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 1 content");
    }

    // Behavior source: NTWizard's interactive navigation contract.
    [Fact]
    public async Task Enter_Key_Advances_To_Next_Enabled_Step() {
        var nextIndex = -1;
        var cut = RenderWizardWithThreeSteps(p => p.Add(w => w.OnNextButtonClicked, EventCallback.Factory.Create<int>(this, index => nextIndex = index)), step2Disabled: true);

        await cut.Find("div.nt-wizard").KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        nextIndex.Should().Be(2);
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 3 content");
    }

    // Behavior source: NTWizard's interactive navigation contract.
    [Fact]
    public async Task NonEnter_Key_Does_Not_Advance_Or_Invoke_Next_Callback() {
        var nextCallbackCount = 0;
        var cut = RenderWizardWithThreeSteps(p => p.Add(w => w.OnNextButtonClicked, EventCallback.Factory.Create<int>(this, _ => nextCallbackCount++)));

        await cut.Find("div.nt-wizard").KeyPressAsync(new KeyboardEventArgs { Key = "Space" });

        nextCallbackCount.Should().Be(0);
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 1 content");
    }

    // Behavior source: NTWizard.OnSubmitCallback XML documentation.
    [Fact]
    public async Task Enter_Key_On_Last_Step_Invokes_Submit_Callback() {
        var submitCount = 0;
        var cut = RenderWizardWithThreeSteps(p => p
            .Add(w => w.ActiveStepIndex, 2)
            .Add(w => w.OnSubmitCallback, EventCallback.Factory.Create(this, () => submitCount++)));

        await cut.Find("div.nt-wizard").KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        submitCount.Should().Be(1);
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 3 content");
    }

    // Behavior source: NTWizard.SubmitButtonDisabled XML documentation.
    [Fact]
    public async Task Enter_Key_On_Last_Step_Does_Not_Submit_When_Disabled() {
        var submitCount = 0;
        var cut = RenderWizardWithThreeSteps(p => p
            .Add(w => w.ActiveStepIndex, 2)
            .Add(w => w.SubmitButtonDisabled, true)
            .Add(w => w.OnSubmitCallback, EventCallback.Factory.Create(this, () => submitCount++)));

        await cut.Find("div.nt-wizard").KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });

        submitCount.Should().Be(0);
        FindButton(cut, "Submit").HasAttribute("disabled").Should().BeTrue();
    }

    // Behavior source: NTWizard.OnStepChanging XML documentation states that returning false cancels navigation.
    [Fact]
    public async Task OnStepChanging_False_Cancels_Next_Navigation_And_PostChange_Callback() {
        NTWizardStepChangeContext? requestedChange = null;
        var changedCount = 0;
        var cut = RenderWizardWithThreeSteps(p => p
            .Add(w => w.OnStepChanging, context => {
                requestedChange = context;
                return Task.FromResult(false);
            })
            .Add(w => w.OnStepChanged, EventCallback.Factory.Create<NTWizardStepChangedEventArgs>(this, _ => changedCount++)));

        await FindNextButton(cut).ClickAsync(new MouseEventArgs());

        requestedChange.Should().NotBeNull();
        requestedChange!.FromStepIndex.Should().Be(0);
        requestedChange.ToStepIndex.Should().Be(1);
        changedCount.Should().Be(0);
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Step 1 content");
    }

    // Behavior source: NTWizard navigation methods document bounded next, previous, and optional-skip progression; a single-step wizard has no valid destination.
    [Fact]
    public async Task SingleStep_Navigation_Boundaries_Are_Idempotent() {
        var nextCount = 0;
        var previousCount = 0;
        var skippedCount = 0;
        var cut = Render<NTWizard>(p => p
            .Add(w => w.OnNextButtonClicked, EventCallback.Factory.Create<int>(this, _ => nextCount++))
            .Add(w => w.OnPreviousButtonClicked, EventCallback.Factory.Create<int>(this, _ => previousCount++))
            .Add(w => w.OnStepSkipped, EventCallback.Factory.Create<int>(this, _ => skippedCount++))
            .AddChildContent<NTWizardStep>(step => {
                step.Add(s => s.Title, "Only step");
                step.Add(s => s.ChildContent, builder => builder.AddContent(0, "Only content"));
            }));

        await cut.Instance.NextStepAsync();
        await cut.Instance.PreviousStepAsync();
        await cut.Instance.SkipStepAsync();

        nextCount.Should().Be(0);
        previousCount.Should().Be(0);
        skippedCount.Should().Be(0);
        cut.Find("div.nt-wizard-content").TextContent.Should().Contain("Only content");
    }

    private IRenderedComponent<NTWizard> RenderWizardWithThreeSteps(Action<ComponentParameterCollectionBuilder<NTWizard>>? configureWizard = null, bool step1Optional = false, bool step2Disabled = false, Func<Task<bool>>? step1ValidateAsync = null) {
        return Render<NTWizard>(p => {
            configureWizard?.Invoke(p);
            p.AddChildContent(builder => {
                builder.OpenComponent<NTWizardStep>(0);
                builder.AddComponentParameter(10, nameof(NTWizardStep.Title), "Step 1");
                builder.AddComponentParameter(20, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 1 content")));
                if (step1Optional) {
                    builder.AddComponentParameter(25, nameof(NTWizardStep.Optional), true);
                }

                if (step1ValidateAsync is not null) {
                    builder.AddComponentParameter(26, nameof(NTWizardStep.ValidateAsync), step1ValidateAsync);
                }

                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(30);
                builder.AddComponentParameter(40, nameof(NTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(50, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                if (step2Disabled) {
                    builder.AddComponentParameter(55, nameof(NTWizardStep.Disabled), true);
                }

                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(60);
                builder.AddComponentParameter(70, nameof(NTWizardStep.Title), "Step 3");
                builder.AddComponentParameter(80, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 3 content")));
                builder.CloseComponent();
            });
        });
    }

    private IRenderedComponent<NTWizard> RenderWizardWithFormStep(WizardRequiredModel model, NTWizardInvalidFormButtonBehavior behavior, RenderFragment<EditContext>? childContent = null) {
        return Render<NTWizard>(p => p
            .Add(w => w.InvalidFormButtonBehavior, behavior)
            .AddChildContent(builder => {
                builder.OpenComponent<NTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(NTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(NTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(NTWizardFormStep.ChildContent), childContent ?? (_ => b => b.AddContent(0, "Form content")));
                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(40);
                builder.AddComponentParameter(50, nameof(NTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(60, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();
            }));
    }

    private IRenderedComponent<NTWizard> RenderWizardWithMinLengthFormStep(WizardMinLengthModel model) {
        return Render<NTWizard>(p => p
            .Add(w => w.InvalidFormButtonBehavior, NTWizardInvalidFormButtonBehavior.DisableButtons)
            .AddChildContent(builder => {
                builder.OpenComponent<NTWizardFormStep>(0);
                builder.AddComponentParameter(10, nameof(NTWizardFormStep.Title), "Form");
                builder.AddComponentParameter(20, nameof(NTWizardFormStep.Model), model);
                builder.AddComponentParameter(30, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => {
                    b.OpenComponent<NTInputText>(0);
                    b.AddComponentParameter(10, nameof(NTInputText.Label), "Name");
                    b.AddComponentParameter(20, nameof(NTInputText.Value), model.Name);
                    b.AddComponentParameter(30, nameof(NTInputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Name = value ?? string.Empty));
                    b.AddComponentParameter(40, nameof(NTInputText.ValueExpression), (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Name));
                    b.CloseComponent();
                }));
                builder.CloseComponent();

                builder.OpenComponent<NTWizardStep>(40);
                builder.AddComponentParameter(50, nameof(NTWizardStep.Title), "Step 2");
                builder.AddComponentParameter(60, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, "Step 2 content")));
                builder.CloseComponent();
            }));
    }

    private static IElement FindNextButton(IRenderedComponent<NTWizard> cut) =>
        FindButton(cut, "Next Step");

    private static IElement FindSkipButton(IRenderedComponent<NTWizard> cut) =>
        FindButton(cut, "Skip");

    private static IElement FindPreviousButton(IRenderedComponent<NTWizard> cut) =>
        FindButton(cut, "Previous Step");

    private static IElement FindButton(IRenderedComponent<NTWizard> cut, string text) =>
        cut.FindAll("button").Single(b => b.TextContent.Contains(text, StringComparison.Ordinal));

    private static IElement FindStepButton(IRenderedComponent<NTWizard> cut, string text) =>
        cut.FindAll("ol.nt-wizard-steps button").Single(b => b.TextContent.Contains(text, StringComparison.Ordinal));

    private static void AddRequiredFormStep(RenderTreeBuilder builder, string title, WizardRequiredModel model, bool validateOnNavigation = true) {
        builder.OpenComponent<NTWizardFormStep>(0);
        builder.AddComponentParameter(1, nameof(NTWizardFormStep.Title), title);
        builder.AddComponentParameter(2, nameof(NTWizardFormStep.Model), model);
        if (!validateOnNavigation) {
            builder.AddComponentParameter(3, nameof(NTWizardFormStep.ValidateOnNavigation), false);
        }

        builder.AddComponentParameter(4, nameof(NTWizardFormStep.ChildContent), (RenderFragment<EditContext>)(_ => b => b.AddContent(0, $"{title} form")));
        builder.CloseComponent();
    }

    private static void AddStep(RenderTreeBuilder builder, string title, string content, bool optional = false, bool skipped = false, bool disabled = false, bool hasError = false, bool completed = false) {
        builder.OpenComponent<NTWizardStep>(0);
        builder.AddComponentParameter(1, nameof(NTWizardStep.Title), title);
        if (optional) {
            builder.AddComponentParameter(2, nameof(NTWizardStep.Optional), true);
        }

        if (skipped) {
            builder.AddComponentParameter(3, nameof(NTWizardStep.Skipped), true);
        }

        if (disabled) {
            builder.AddComponentParameter(4, nameof(NTWizardStep.Disabled), true);
        }

        if (hasError) {
            builder.AddComponentParameter(5, nameof(NTWizardStep.HasError), true);
        }

        if (completed) {
            builder.AddComponentParameter(6, nameof(NTWizardStep.Completed), true);
        }

        builder.AddComponentParameter(7, nameof(NTWizardStep.ChildContent), (RenderFragment)(b => b.AddContent(0, content)));
        builder.CloseComponent();
    }

    private sealed class WizardSkipCallbackHost : ComponentBase {

        public int SkippedIndex { get; private set; } = -1;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTWizard>(0);
            builder.AddComponentParameter(1, nameof(NTWizard.OnStepSkipped), EventCallback.Factory.Create<int>(this, HandleStepSkipped));
            builder.AddComponentParameter(2, nameof(NTWizard.ChildContent), (RenderFragment)(contentBuilder => {
                AddStep(contentBuilder, "Optional", "Optional content", optional: true);
                AddStep(contentBuilder, "Disabled", "Disabled content", disabled: true);
                AddStep(contentBuilder, "Review", "Review content");
            }));
            builder.CloseComponent();
            builder.AddContent(3, $"Skipped: {SkippedIndex}");
        }

        private void HandleStepSkipped(int stepIndex) {
            SkippedIndex = stepIndex;
        }
    }

    private sealed class WizardRequiredModel {

        [Required]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class WizardMinLengthModel {

        [Required]
        [MinLength(5)]
        public string Name { get; set; } = string.Empty;
    }
}
