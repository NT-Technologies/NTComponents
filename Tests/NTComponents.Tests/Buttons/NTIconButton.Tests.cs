using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class NTIconButton_Tests : BunitContext {
    private static NTIcon SampleIcon => MaterialIcon.Menu;

    public NTIconButton_Tests() => RippleTestingUtility.SetupRippleEffectModule(this);

    [Fact]
    public void Default_Render_Uses_Standard_Variant_Button_Type_And_AriaLabel() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu"));

        var button = cut.Find("button");

        button.GetAttribute("class")!.Should().Contain("nt-icon-button-standard");
        button.GetAttribute("type")!.Should().Be("button");
        button.GetAttribute("aria-label")!.Should().Be("Open menu");
    }

    [Fact]
    public void Badge_Is_Not_Rendered_By_Default() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open inbox"));

        cut.FindAll(".nt-badge").Should().BeEmpty();
        cut.Find("button").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void ShowBadge_Renders_Badge_Outside_AriaHidden_Icon_And_Describes_Button() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open inbox")
            .Add(x => x.ShowBadge, true)
            .Add(x => x.BadgeContent, "3")
            .Add(x => x.BadgeAriaLabel, "3 unread messages"));

        var button = cut.Find("button");
        var badge = button.Children.Single(child => child.ClassList.Contains("nt-badge"));

        badge.ClassList.Should().Contain("nt-badge-large");
        badge.Id.Should().NotBeNullOrWhiteSpace();
        button.GetAttribute("aria-describedby").Should().Be(badge.Id);
        button.QuerySelector(".nt-icon-button-icon .nt-badge").Should().BeNull();
        badge.QuerySelector(".nt-badge-accessible-label")!.TextContent.Should().Be("3 unread messages");
    }

    [Fact]
    public void ShowBadge_Without_Content_Renders_Small_Badge() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open inbox")
            .Add(x => x.ShowBadge, true));

        cut.Find(".nt-badge").ClassList.Should().Contain("nt-badge-small");
    }

    [Fact]
    public void ShowBadge_Appends_Badge_Id_To_Existing_AriaDescribedBy() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open inbox")
            .Add(x => x.ShowBadge, true)
            .AddUnmatched("aria-describedby", "inbox-help"));

        var badgeId = cut.Find(".nt-badge").Id;

        cut.Find("button").GetAttribute("aria-describedby").Should().Be($"inbox-help {badgeId}");
    }

    [Fact]
    public void Hidden_Badge_Preserves_Existing_AriaDescribedBy() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open inbox")
            .AddUnmatched("aria-describedby", "inbox-help"));

        cut.Find("button").GetAttribute("aria-describedby").Should().Be("inbox-help");
    }

    [Fact]
    public void Requires_Icon_Parameter() {
        var render = () => Render<NTIconButton>(parameters => parameters.Add(x => x.AriaLabel, "Open menu"));

        render.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Requires_AriaLabel_Parameter() {
        var render = () => Render<NTIconButton>(parameters => parameters.Add(x => x.Icon, SampleIcon));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*NTIconButton requires a non-empty AriaLabel*");
    }

    [Fact]
    public void Does_Not_Render_Visible_Label() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu"));

        var button = cut.Find("button");

        button.TextContent.Should().NotContain("Open menu");
        button.QuerySelector(".nt-icon-button-icon")!.TextContent.Should().Be(MaterialIcon.Menu.Icon);
        cut.FindAll(".nt-button-label").Should().BeEmpty();
    }

    [Fact]
    public void Icon_Renders_In_AriaHidden_Wrapper() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu"));

        var iconWrapper = cut.Find(".nt-icon-button-icon");

        iconWrapper.GetAttribute("aria-hidden")!.Should().Be("true");
        iconWrapper.QuerySelector(".nt-icon")!.TextContent.Should().Be(MaterialIcon.Menu.Icon);
    }

    [Fact]
    public void NonToggle_Button_Does_Not_Render_AriaPressed() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu"));

        cut.Find("button").HasAttribute("aria-pressed").Should().BeFalse();
    }

    [Fact]
    public void Toggle_Button_Renders_AriaPressed_State() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, MaterialIcon.Favorite)
            .Add(x => x.AriaLabel, "Favorite item")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, true));

        var button = cut.Find("button");

        button.GetAttribute("aria-pressed")!.Should().Be("true");
        button.GetAttribute("class")!.Should().Contain("nt-icon-button-selected");
    }

    [Fact]
    public void Text_Toggle_Button_Is_Supported_As_Standard_Icon_Button() {
        var render = () => Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, MaterialIcon.FavoriteBorder)
            .Add(x => x.AriaLabel, "Favorite item")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Variant, NTButtonVariant.Text));

        render.Should().NotThrow();
    }

    [Fact]
    public void Click_Invokes_OnClickCallback() {
        var clicked = 0;
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));

        cut.Find("button").Click();

        clicked.Should().Be(1);
    }

    [Fact]
    public void ShowProgress_Renders_Progress_And_Disables_Button() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.ShowProgress, true));

        var button = cut.Find("button");

        button.HasAttribute("disabled").Should().BeTrue();
        button.GetAttribute("class")!.Should().Contain("nt-button-progress-active");
        button.GetAttribute("class")!.Should().NotContain("nt-disabled");
        cut.Find(".nt-button-progress .nt-progress").GetAttribute("class")!.Should().Contain("nt-progress-ring");
    }

    [Fact]
    public void Disabled_Click_Does_Not_Invoke_Callback() {
        var clicked = 0;
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Disabled, true)
            .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));

        cut.Find("button").Click();

        clicked.Should().Be(0);
    }

    [Fact]
    public void Toggle_Click_Flips_Selected_And_Invokes_SelectedChanged_Before_ClickCallback() {
        var selectedChangedValue = false;
        var callbackObservedSelected = false;
        IRenderedComponent<NTIconButton>? cut = null;
        cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, MaterialIcon.FavoriteBorder)
            .Add(x => x.AriaLabel, "Favorite item")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, false)
            .Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, value => selectedChangedValue = value))
            .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => callbackObservedSelected = cut!.Instance.Selected)));

        cut.Find("button").Click();

        selectedChangedValue.Should().BeTrue();
        callbackObservedSelected.Should().BeTrue();
    }

    [Fact]
    public void EnableRipple_False_Does_Not_Render_Ripple_Component() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.EnableRipple, false));

        cut.FindAll(".nt-button-ripple-host").Should().BeEmpty();
        cut.Markup.Should().NotContain("nt-button-ripple-host");
        cut.Markup.Should().Contain("startButtonInteraction");
    }

    [Fact]
    public void EnableRipple_True_Renders_RippleHost() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu"));

        cut.Find(".nt-button-ripple-host").Should().NotBeNull();
    }

    [Theory]
    [InlineData(NTButtonVariant.Elevated, "nt-icon-button-elevated")]
    [InlineData(NTButtonVariant.Filled, "nt-icon-button-filled")]
    [InlineData(NTButtonVariant.Tonal, "nt-icon-button-tonal")]
    [InlineData(NTButtonVariant.Outlined, "nt-icon-button-outlined")]
    [InlineData(NTButtonVariant.Text, "nt-icon-button-standard")]
    public void Variant_Adds_Variant_Class(NTButtonVariant variant, string expectedClass) {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Variant, variant));

        cut.Find("button").GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Fact]
    public void Default_Colors_Come_From_Standard_Variant_When_No_Overrides_Are_Set() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu"));

        var style = cut.Find("button").GetAttribute("style");

        style.Should().Contain("--nt-icon-button-bg:var(--nt-color-transparent)");
        style.Should().Contain("--nt-icon-button-fg:var(--nt-color-on-surface-variant)");
    }

    [Theory]
    [InlineData(NTButtonVariant.Text, false, "transparent", "on-surface-variant")]
    [InlineData(NTButtonVariant.Text, true, "transparent", "primary")]
    [InlineData(NTButtonVariant.Filled, false, "surface-container-highest", "primary")]
    [InlineData(NTButtonVariant.Filled, true, "primary", "on-primary")]
    [InlineData(NTButtonVariant.Tonal, false, "secondary-container", "on-secondary-container")]
    [InlineData(NTButtonVariant.Tonal, true, "secondary", "on-secondary")]
    [InlineData(NTButtonVariant.Outlined, false, "transparent", "on-surface-variant")]
    [InlineData(NTButtonVariant.Outlined, true, "inverse-surface", "inverse-on-surface")]
    public void Toggle_Button_Default_Colors_Come_From_Selected_State(NTButtonVariant variant, bool selected, string expectedBackground, string expectedText) {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, selected)
            .Add(x => x.Variant, variant));

        var style = cut.Find("button").GetAttribute("style");

        style.Should().Contain($"--nt-icon-button-bg:var(--nt-color-{expectedBackground})");
        style.Should().Contain($"--nt-icon-button-fg:var(--nt-color-{expectedText})");
    }

    [Fact]
    public void Custom_Colors_Render_As_Css_Variables() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.BackgroundColor, NTColor.SecondaryContainer)
            .Add(x => x.TextColor, NTColor.OnSecondaryContainer));

        var style = cut.Find("button").GetAttribute("style");

        style.Should().Contain("--nt-icon-button-bg:var(--nt-color-secondary-container)");
        style.Should().Contain("--nt-icon-button-fg:var(--nt-color-on-secondary-container)");
    }

    [Fact]
    public void Filled_Button_With_Elevation_Throws() {
        var render = () => Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.Elevation, NTElevation.Lowest));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Filled icon buttons must use None Elevation*");
    }

    [Fact]
    public void Text_Button_With_Visible_Background_Throws() {
        var render = () => Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.BackgroundColor, NTColor.Primary));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Text icon buttons must use a transparent BackgroundColor*");
    }

    [Fact]
    public void Filled_Button_With_Transparent_Background_Throws() {
        var render = () => Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.BackgroundColor, NTColor.Transparent));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Filled icon buttons must use a visible container BackgroundColor*");
    }

    [Fact]
    public void Selected_Outlined_Toggle_With_Transparent_Background_Throws() {
        var render = () => Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Variant, NTButtonVariant.Outlined)
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, true)
            .Add(x => x.BackgroundColor, NTColor.Transparent));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Outlined selected toggle icon buttons must use a visible container BackgroundColor*");
    }

    [Fact]
    public void Transparent_TextColor_Throws() {
        var render = () => Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.TextColor, NTColor.Transparent));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*TextColor must be a visible icon color*");
    }

    [Fact]
    public void Elevated_Button_With_No_Elevation_Throws() {
        var render = () => Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Variant, NTButtonVariant.Elevated)
            .Add(x => x.Elevation, NTElevation.None));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Elevated icon buttons must use a non-zero Elevation*");
    }

    [Fact]
    public void Size_Width_And_Shape_Add_Classes() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.ButtonSize, NTSize.Largest)
            .Add(x => x.Width, NTIconButtonAppearance.Wide)
            .Add(x => x.Shape, ButtonShape.Square));

        var classes = cut.Find("button").GetAttribute("class")!;

        classes.Should().Contain("nt-size-xl");
        classes.Should().Contain("nt-icon-button-width-wide");
        classes.Should().Contain("nt-icon-button-shape-square");
    }

    [Fact]
    public void Toggle_Button_Uses_Round_Unselected_And_Square_Selected_Shapes() {
        var unselected = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, false)
            .Add(x => x.Shape, ButtonShape.Square));
        var selected = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, true)
            .Add(x => x.Shape, ButtonShape.Round));

        unselected.Find("button").GetAttribute("class")!.Should().Contain("nt-icon-button-shape-round");
        selected.Find("button").GetAttribute("class")!.Should().Contain("nt-icon-button-shape-square");
    }

    [Fact]
    public void Variant_Rerender_Recomputes_Default_Colors_And_Elevation_When_Not_Provided() {
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Variant, NTButtonVariant.Filled));

        var rerender = () => cut.Render(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Open menu")
            .Add(x => x.Variant, NTButtonVariant.Text));

        rerender.Should().NotThrow();
        cut.Find("button").GetAttribute("style").Should().Contain("--nt-icon-button-bg:var(--nt-color-transparent)");
    }

    [Fact]
    public void SelectedChanged_Supports_Bind_Selected_Shape_For_NTIconButton() {
        var selected = false;
        var cut = Render<NTIconButton>(parameters => parameters
            .Add(x => x.Icon, MaterialIcon.FavoriteBorder)
            .Add(x => x.AriaLabel, "Favorite item")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, selected)
            .Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, value => selected = value)));

        cut.Find("button").Click();

        selected.Should().BeTrue();
        cut.Instance.Selected.Should().BeTrue();
    }
}
