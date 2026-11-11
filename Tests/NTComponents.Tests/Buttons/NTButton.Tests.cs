using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class NTButton_Tests : BunitContext {
    public NTButton_Tests() => RippleTestingUtility.SetupRippleEffectModule(this);

    [Fact]
    public void Default_Render_Uses_Filled_Variant_And_Button_Type() {
        var cut = Render<NTButton>(parameters => parameters.Add(x => x.Label, "Save"));
        var button = cut.Find("button");

        button.GetAttribute("class")!.Should().Contain("nt-button-filled");
        button.GetAttribute("type")!.Should().Be("button");
    }

    [Fact]
    public void Toggle_Button_Renders_AriaPressed_State() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Favorite")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, true)
            .Add(x => x.Variant, NTButtonVariant.Filled));

        var button = cut.Find("button");

        button.GetAttribute("aria-pressed")!.Should().Be("true");
        button.GetAttribute("class")!.Should().Contain("nt-button-selected");
    }

    [Fact]
    public void NonToggle_Button_Does_Not_Render_AriaPressed() {
        var cut = Render<NTButton>(parameters => parameters.Add(x => x.Label, "Continue"));

        cut.Find("button").HasAttribute("aria-pressed").Should().BeFalse();
    }

    [Fact]
    public void LeadingIcon_Renders_Before_Label() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Edit")
            .Add(x => x.LeadingIcon, MaterialIcon.Edit));

        cut.Find(".nt-button-icon .nt-icon").TextContent.Should().Be(MaterialIcon.Edit.Icon);
        cut.Find("button").TextContent.Should().Contain("Edit");
    }

    [Fact]
    public void EnableRipple_False_Does_Not_Render_Ripple_Component() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "No ripple")
            .Add(x => x.EnableRipple, false));

        cut.FindAll(".nt-button-ripple-host").Should().BeEmpty();
        cut.Markup.Should().NotContain("nt-button-ripple-host");
    }

    [Fact]
    public void Click_Invokes_OnClickCallback() {
        var clicked = 0;
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Click")
            .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));

        cut.Find("button").Click();

        clicked.Should().Be(1);
    }

    [Fact]
    public void ShowProgress_Renders_Indeterminate_Ring_Progress_And_Disables_Button() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .Add(x => x.ShowProgress, true));

        var button = cut.Find("button");
        var progress = cut.Find(".nt-button-progress .nt-progress");

        button.HasAttribute("disabled").Should().BeTrue();
        button.GetAttribute("class")!.Should().Contain("nt-button-progress-active");
        button.GetAttribute("class")!.Should().NotContain("nt-disabled");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-ring");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-indeterminate");
        progress.HasAttribute("aria-valuenow").Should().BeFalse();
    }

    [Fact]
    public void ProgressValue_Renders_Determinate_Linear_Progress() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Upload")
            .Add(x => x.ShowProgress, true)
            .Add(x => x.ProgressVariant, NTProgressVariant.Linear)
            .Add(x => x.ProgressValue, 35));

        var progress = cut.Find(".nt-button-progress .nt-progress");

        progress.GetAttribute("class")!.Should().Contain("nt-progress-linear");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-determinate");
        progress.GetAttribute("aria-valuenow")!.Should().Be("35");
    }

    [Fact]
    public void ShowProgress_Click_Does_Not_Invoke_Callback() {
        var clicked = 0;
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Saving")
            .Add(x => x.ShowProgress, true)
            .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));

        cut.Find("button").Click();

        clicked.Should().Be(0);
    }

    [Fact]
    public void Toggle_Click_Invokes_SelectedChanged_For_Bind_Selected() {
        var selected = false;
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Favorite")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, selected)
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, value => selected = value)));

        cut.Find("button").Click();

        selected.Should().BeTrue();
        cut.Instance.Selected.Should().BeTrue();
    }

    [Fact]
    public void Toggle_Button_Uses_Round_Unselected_And_Square_Selected_Shapes() {
        var unselected = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Unselected")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, false)
            .Add(x => x.Shape, ButtonShape.Square));
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Selected")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, true)
            .Add(x => x.Shape, ButtonShape.Round));

        var unselectedButton = unselected.Find("button");
        var selectedButton = cut.Find("button");

        unselectedButton.GetAttribute("class")!.Should().Contain("nt-button-shape-round");
        unselectedButton.GetAttribute("class")!.Should().NotContain("nt-button-shape-square");
        selectedButton.GetAttribute("class")!.Should().Contain("nt-button-shape-square");
        selectedButton.GetAttribute("class")!.Should().NotContain("nt-button-shape-round");
    }

    [Fact]
    public void Text_Toggle_Button_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Variant, NTButtonVariant.Text));

        render.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Text_Button_With_Visible_BackgroundColor_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Text)
            .Add(x => x.BackgroundColor, NTColor.Primary));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Text buttons must use a transparent BackgroundColor*");
    }

    [Theory]
    [InlineData(NTButtonVariant.Elevated)]
    [InlineData(NTButtonVariant.Filled)]
    [InlineData(NTButtonVariant.Tonal)]
    public void Contained_Button_With_Transparent_BackgroundColor_Throws(NTButtonVariant variant) {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, NTColor.Transparent));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} buttons must use a visible container BackgroundColor*");
    }

    [Fact]
    public void Filled_Button_With_Elevation_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.Elevation, NTElevation.Lowest));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Filled buttons must use None Elevation*");
    }

    [Fact]
    public void Elevated_Button_With_No_Elevation_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Elevated)
            .Add(x => x.Elevation, NTElevation.None));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Elevated buttons must use a non-zero Elevation*");
    }

    [Fact]
    public void Empty_Label_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, string.Empty));

        render.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Smallest_Size_Adds_Size_Class() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "XS")
            .Add(x => x.ButtonSize, NTSize.Smallest));

        cut.Find("button").GetAttribute("class")!.Should().Contain("nt-size-xs");
    }

    [Fact]
    public void EnableRipple_True_Renders_RippleHost() {
        var cut = Render<NTButton>(parameters => parameters.Add(x => x.Label, "Ripple"));

        cut.Find(".nt-button-ripple-host").Should().NotBeNull();
    }

    [Fact]
    public void Custom_Colors_Render_As_Css_Variables() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Styled")
            .Add(x => x.BackgroundColor, NTColor.SecondaryContainer)
            .Add(x => x.TextColor, NTColor.OnSecondaryContainer));

        var style = cut.Find("button").GetAttribute("style");

        style.Should().Contain("--nt-button-bg:var(--nt-color-secondary-container)");
        style.Should().Contain("--nt-button-fg:var(--nt-color-on-secondary-container)");
    }

    [Fact]
    public void Default_Colors_Come_From_Variant_When_No_Overrides_Are_Set() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Outlined")
            .Add(x => x.Variant, NTButtonVariant.Outlined));

        var button = cut.Find("button");
        var style = button.GetAttribute("style");

        button.GetAttribute("class")!.Should().Contain("nt-button-outlined");
        style.Should().Contain("--nt-button-bg:var(--nt-color-transparent)");
        style.Should().Contain("--nt-button-fg:var(--nt-color-primary)");
    }

    [Theory]
    [InlineData(NTButtonVariant.Elevated, false, "surface-container-low", "primary")]
    [InlineData(NTButtonVariant.Elevated, true, "primary", "on-primary")]
    [InlineData(NTButtonVariant.Filled, false, "surface-container", "on-surface-variant")]
    [InlineData(NTButtonVariant.Filled, true, "primary", "on-primary")]
    [InlineData(NTButtonVariant.Tonal, false, "secondary-container", "on-secondary-container")]
    [InlineData(NTButtonVariant.Tonal, true, "secondary", "on-secondary")]
    [InlineData(NTButtonVariant.Outlined, false, "transparent", "on-surface-variant")]
    [InlineData(NTButtonVariant.Outlined, true, "inverse-surface", "inverse-on-surface")]
    public void Toggle_Button_Default_Colors_Come_From_Selected_State(NTButtonVariant variant, bool selected, string expectedBackground, string expectedText) {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Toggle")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, selected)
            .Add(x => x.Variant, variant));

        var style = cut.Find("button").GetAttribute("style");

        style.Should().Contain($"--nt-button-bg:var(--nt-color-{expectedBackground})");
        style.Should().Contain($"--nt-button-fg:var(--nt-color-{expectedText})");
    }

    [Fact]
    public void Variant_Rerender_Recomputes_Default_Colors_And_Elevation_When_Not_Provided() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Dynamic")
            .Add(x => x.Variant, NTButtonVariant.Filled));

        var rerender = () => cut.Render(parameters => parameters
            .Add(x => x.Label, "Dynamic")
            .Add(x => x.Variant, NTButtonVariant.Text));

        rerender.Should().NotThrow();
        cut.Find("button").GetAttribute("style").Should().Contain("--nt-button-bg:var(--nt-color-transparent)");
    }
}
