using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Buttons;

public class NTFabButton_Tests : BunitContext {
    private static NTIcon SampleIcon => MaterialIcon.Add;

    [Fact]
    public void Default_Render_Uses_IconOnly_Mode_Small_Size_Elevation_And_Button_Type() {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item"));

        var button = cut.Find("button");
        var classes = button.GetAttribute("class")!;

        classes.Should().Contain("nt-fab-button");
        classes.Should().Contain("nt-fab-button-icon-only");
        classes.Should().Contain("nt-fab-button-placement-inline");
        classes.Should().Contain("nt-size-s");
        classes.Should().Contain("nt-elevation-medium");
        button.GetAttribute("type")!.Should().Be("button");
        button.GetAttribute("aria-label")!.Should().Be("Create item");
    }

    [Fact]
    public void Requires_Icon_Parameter() {
        var render = () => Render<NTFabButton>(parameters => parameters.Add(x => x.AriaLabel, "Create item"));

        render.Should().Throw<ArgumentNullException>()
            .WithMessage("*NTFabButton requires a non-null Icon parameter*");
    }

    [Fact]
    public void IconOnly_Mode_Requires_AriaLabel() {
        var render = () => Render<NTFabButton>(parameters => parameters.Add(x => x.Icon, SampleIcon));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*Icon-only NTFabButton requires a non-empty AriaLabel*");
    }

    [Fact]
    public void Label_Renders_Extended_Mode_Without_AriaLabel() {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.Label, "Create"));

        var button = cut.Find("button");
        var classes = button.GetAttribute("class")!;

        classes.Should().Contain("nt-fab-button-extended");
        classes.Should().NotContain("nt-fab-button-icon-only");
        button.HasAttribute("aria-label").Should().BeFalse();
        cut.Find(".nt-fab-button-label").TextContent.Should().Be("Create");
        cut.Find(".nt-fab-button-icon .nt-icon").TextContent.Should().Be(MaterialIcon.Add.Icon);
    }

    [Fact]
    public void Label_With_LineBreak_Throws() {
        var render = () => Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.Label, "Create\nitem"));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*Label must not contain line breaks*");
    }

    [Fact]
    public void Custom_Colors_Render_As_Css_Variables() {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.BackgroundColor, NTColor.SecondaryContainer)
            .Add(x => x.TextColor, NTColor.OnSecondaryContainer));

        var style = cut.Find("button").GetAttribute("style");

        style.Should().Contain("--nt-fab-bg:var(--nt-color-secondary-container)");
        style.Should().Contain("--nt-fab-fg:var(--nt-color-on-secondary-container)");
    }

    [Fact]
    public void Default_Colors_Come_From_Scss_When_No_Overrides_Are_Set() {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item"));

        var style = cut.Find("button").GetAttribute("style");

        style.Should().NotContain("--nt-fab-bg");
        style.Should().NotContain("--nt-fab-fg");
    }

    [Fact]
    public void Transparent_BackgroundColor_Throws() {
        var render = () => Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.BackgroundColor, NTColor.Transparent));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*BackgroundColor must be a visible container color*");
    }

    [Fact]
    public void Transparent_TextColor_Throws() {
        var render = () => Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.TextColor, NTColor.Transparent));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*TextColor must be a visible content color*");
    }

    [Theory]
    [InlineData(NTSize.Smallest, "nt-size-s")]
    [InlineData(NTSize.Small, "nt-size-s")]
    [InlineData(NTSize.Medium, "nt-size-m")]
    [InlineData(NTSize.Large, "nt-size-l")]
    [InlineData(NTSize.Largest, "nt-size-l")]
    public void ButtonSize_Uses_Supported_Fab_Size(NTSize suppliedSize, string expectedClass) {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.ButtonSize, suppliedSize));

        var classes = cut.Find("button").GetAttribute("class")!;

        classes.Should().Contain(expectedClass);
        classes.Should().NotContain("nt-size-xs");
        classes.Should().NotContain("nt-size-xl");
    }

    [Fact]
    public void Elevation_Can_Use_Lowest_Material_Elevation_Set() {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.Elevation, NTElevation.Lowest));

        cut.Find("button").GetAttribute("class")!.Should().Contain("nt-elevation-lowest");
    }

    [Theory]
    [InlineData(NTFabButtonPlacement.Inline, "nt-fab-button-placement-inline")]
    [InlineData(NTFabButtonPlacement.LowerRight, "nt-fab-button-placement-lower-right")]
    [InlineData(NTFabButtonPlacement.LowerLeft, "nt-fab-button-placement-lower-left")]
    [InlineData(NTFabButtonPlacement.UpperRight, "nt-fab-button-placement-upper-right")]
    [InlineData(NTFabButtonPlacement.UpperLeft, "nt-fab-button-placement-upper-left")]
    public void Placement_Renders_Placement_Class(NTFabButtonPlacement placement, string expectedClass) {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.Placement, placement));

        cut.Find("button").GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Fact]
    public void Invalid_Placement_Throws() {
        var render = () => Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.Placement, (NTFabButtonPlacement)999));

        render.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Placement");
    }

    [Fact]
    public void EnableRipple_False_Does_Not_Render_Ripple_Component() {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.EnableRipple, false));

        cut.FindAll(".nt-button-ripple-host").Should().BeEmpty();
        cut.Markup.Should().NotContain("nt-button-ripple-host");
        cut.Markup.Should().NotContain("startButtonInteraction");
        cut.Markup.Should().NotContain("startRippleHost");
    }

    [Fact]
    public void EnableRipple_True_Renders_RippleHost() {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item"));

        cut.Find(".nt-button-ripple-host").Should().NotBeNull();
        cut.Markup.Should().Contain("startRippleHost");
        cut.Markup.Should().NotContain("startButtonInteraction");
    }

    [Fact]
    public void Click_Invokes_OnClickCallback() {
        var clicked = 0;
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));

        cut.Find("button").Click();

        clicked.Should().Be(1);
    }

    [Fact]
    public void ShowProgress_Renders_Progress_And_Disables_Button() {
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
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
        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.Disabled, true)
            .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));

        var button = cut.Find("button");
        button.Click();

        clicked.Should().Be(0);
        button.HasAttribute("disabled").Should().BeTrue();
        button.GetAttribute("class")!.Should().Contain("nt-disabled");
    }

    [Fact]
    public void Additional_Class_And_Style_Are_Merged() {
        var attrs = new Dictionary<string, object> {
            ["class"] = "extra-class",
            ["style"] = "margin:3px"
        };

        var cut = Render<NTFabButton>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create item")
            .Add(x => x.AdditionalAttributes, attrs));

        var button = cut.Find("button");

        button.GetAttribute("class")!.Should().Contain("extra-class");
        button.GetAttribute("style")!.Should().Contain("margin:3px");
    }
}
