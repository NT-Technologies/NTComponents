using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Animation;

public class NTAnimation_Tests : BunitContext {
    public static IEnumerable<object[]> AllAnimationTypes => Enum.GetValues<NTAnimationType>().Select(value => new object[] { value });

    public static IEnumerable<object[]> AllTags => [
        [NTAnimationTag.Div, "div"],
        [NTAnimationTag.Section, "section"],
        [NTAnimationTag.Article, "article"],
        [NTAnimationTag.UnorderedList, "ul"],
        [NTAnimationTag.OrderedList, "ol"],
        [NTAnimationTag.ListItem, "li"]
    ];

    public NTAnimation_Tests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<NTAnimation> RenderAnimation(Action<ComponentParameterCollectionBuilder<NTAnimation>>? configure = null) =>
        Render<NTAnimation>(parameters => {
            configure?.Invoke(parameters);
            parameters.AddChildContent("<span>Content</span>");
        });

    private EventCallback Callback(Action action) => EventCallback.Factory.Create(this, action);

    [Theory]
    [MemberData(nameof(AllAnimationTypes))]
    public void Adds_Class_For_Each_Animation_Type(NTAnimationType animation) {
        var cut = RenderAnimation(p => p.Add(c => c.Animation, animation));

        var classes = cut.Find(".nt-animation").ClassList;
        classes.Should().Contain("nt-animation").And.Contain(animation.ToCssClass());
    }

    [Fact]
    public void Adds_Stagger_Class_Only_When_Stagger_Is_Positive() {
        RenderAnimation().Find(".nt-animation").ClassList.Should().NotContain("nt-animation-stagger");
        RenderAnimation(p => p.Add(c => c.Stagger, TimeSpan.FromMilliseconds(80))).Find(".nt-animation").ClassList.Should().Contain("nt-animation-stagger");
    }

    [Fact]
    public void Default_Easings_Add_No_Easing_Classes() {
        var classes = RenderAnimation().Find(".nt-animation").ClassList;

        classes.Where(c => c.StartsWith("nt-animation-enter-") || c.StartsWith("nt-animation-exit-")).Should().BeEmpty();
    }

    [Fact]
    public void Non_Default_Easings_Add_Enter_And_Exit_Classes() {
        var cut = RenderAnimation(p => p
            .Add(c => c.EnterEasing, NTMotionEasing.Standard)
            .Add(c => c.ExitEasing, NTMotionEasing.Emphasized));

        cut.Find(".nt-animation").ClassList.Should().Contain("nt-animation-enter-standard").And.Contain("nt-animation-exit-emphasized");
    }

    [Fact]
    public void Default_Parameters_Emit_No_Animation_Variables() {
        var cut = RenderAnimation();

        (cut.Find(".nt-animation").GetAttribute("style") ?? string.Empty).Should().NotContain("--nt-animation");
    }

    [Fact]
    public void Non_Default_Parameters_Emit_Variables() {
        var cut = RenderAnimation(p => p
            .Add(c => c.EnterDuration, NTMotionDuration.Ms500)
            .Add(c => c.ExitDuration, NTMotionDuration.Ms150)
            .Add(c => c.Delay, TimeSpan.FromMilliseconds(75))
            .Add(c => c.Stagger, TimeSpan.FromMilliseconds(40))
            .Add(c => c.Distance, "32px"));

        var style = (cut.Find(".nt-animation").GetAttribute("style") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Replace(" ", string.Empty))
            .ToArray();
        style.Should().BeEquivalentTo(
            "--nt-animation-enter-duration:500ms",
            "--nt-animation-exit-duration:150ms",
            "--nt-animation-delay:75ms",
            "--nt-animation-stagger:40ms",
            "--nt-animation-distance:32px");
    }

    [Fact]
    public void Negative_Delay_And_Blank_Distance_Are_Omitted() {
        var cut = RenderAnimation(p => p.Add(c => c.Delay, TimeSpan.FromMilliseconds(-50)).Add(c => c.Distance, "  "));

        (cut.Find(".nt-animation").GetAttribute("style") ?? string.Empty).Should().NotContain("--nt-animation");
    }

    [Fact]
    public void Visible_Trigger_Renders_Observer_Attributes_And_Page_Script() {
        var cut = RenderAnimation(p => p
            .Add(c => c.Repeat, NTAnimationRepeat.Reverse)
            .Add(c => c.Threshold, 0.75)
            .Add(c => c.RootMargin, "0px 0px -10% 0px"));

        var element = cut.Find(".nt-animation");
        element.GetAttribute("data-nt-animation-trigger").Should().Be("visible");
        element.GetAttribute("data-nt-animation-repeat").Should().Be("reverse");
        element.GetAttribute("data-nt-animation-threshold").Should().Be("0.75");
        element.GetAttribute("data-nt-animation-root-margin").Should().Be("0px 0px -10% 0px");
        element.HasAttribute("data-nt-animation-notify").Should().BeFalse();
        element.HasAttribute("data-nt-animation-state").Should().BeFalse();
        cut.Find("nt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Animation/NTAnimation.razor.js");
    }

    [Fact]
    public void Defaults_Render_Once_Repeat_Threshold_And_Root_Margin() {
        var element = RenderAnimation().Find(".nt-animation");

        element.GetAttribute("data-nt-animation-repeat").Should().Be("once");
        element.GetAttribute("data-nt-animation-threshold").Should().Be("0.35");
        element.GetAttribute("data-nt-animation-root-margin").Should().Be("0px");
    }

    [Fact]
    public void Notify_Attribute_Is_Rendered_When_OnEntered_Is_Bound() {
        var cut = RenderAnimation(p => p.Add(c => c.OnEntered, Callback(() => { })));

        cut.Find(".nt-animation").HasAttribute("data-nt-animation-notify").Should().BeTrue();
    }

    [Fact]
    public void Notify_Attribute_Is_Rendered_When_OnExited_Is_Bound() {
        var cut = RenderAnimation(p => p.Add(c => c.OnExited, Callback(() => { })));

        cut.Find(".nt-animation").HasAttribute("data-nt-animation-notify").Should().BeTrue();
    }

    [Theory]
    [InlineData(double.NaN, "0")]
    [InlineData(-1d, "0")]
    [InlineData(0d, "0")]
    [InlineData(0.5d, "0.5")]
    [InlineData(2d, "1")]
    public void Threshold_Is_Clamped_To_Unit_Interval(double threshold, string expected) {
        var cut = RenderAnimation(p => p.Add(c => c.Threshold, threshold));

        cut.Find(".nt-animation").GetAttribute("data-nt-animation-threshold").Should().Be(expected);
    }

    [Fact]
    public void Render_Trigger_Emits_Only_Trigger_Attribute_And_No_Page_Script() {
        var cut = RenderAnimation(p => p.Add(c => c.Trigger, NTAnimationTrigger.Render).Add(c => c.OnEntered, Callback(() => { })));

        var element = cut.Find(".nt-animation");
        element.GetAttribute("data-nt-animation-trigger").Should().Be("render");
        element.Attributes.Select(a => a.Name).Where(n => n.StartsWith("data-nt-animation")).Should().BeEquivalentTo(["data-nt-animation-trigger"]);
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Fact]
    public void Manual_Trigger_Shown_Renders_Visible_State_Without_Inert_Or_Page_Script() {
        var cut = RenderAnimation(p => p.Add(c => c.Trigger, NTAnimationTrigger.Manual).Add(c => c.Shown, true));

        var element = cut.Find(".nt-animation");
        element.GetAttribute("data-nt-animation-trigger").Should().Be("manual");
        element.GetAttribute("data-nt-animation-state").Should().Be("visible");
        element.HasAttribute("inert").Should().BeFalse();
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Fact]
    public void Manual_Trigger_Hidden_Renders_Hidden_State_And_Inert_And_Still_Renders_Content() {
        var cut = RenderAnimation(p => p.Add(c => c.Trigger, NTAnimationTrigger.Manual).Add(c => c.Shown, false));

        var element = cut.Find(".nt-animation");
        element.GetAttribute("data-nt-animation-state").Should().Be("hidden");
        element.HasAttribute("inert").Should().BeTrue();
        element.QuerySelector("span")!.TextContent.Should().Be("Content");
    }

    [Fact]
    public void Manual_Trigger_Toggling_Shown_Updates_State_And_Inert() {
        var cut = RenderAnimation(p => p.Add(c => c.Trigger, NTAnimationTrigger.Manual).Add(c => c.Shown, false));

        cut.Render(p => p.Add(c => c.Shown, true));

        var element = cut.Find(".nt-animation");
        element.GetAttribute("data-nt-animation-state").Should().Be("visible");
        element.HasAttribute("inert").Should().BeFalse();
    }

    [Theory]
    [InlineData(NTAnimationTrigger.Visible)]
    [InlineData(NTAnimationTrigger.Render)]
    [InlineData(NTAnimationTrigger.Manual)]
    public void Disabled_Renders_Content_Without_Any_Animation_Attributes_Or_Page_Script(NTAnimationTrigger trigger) {
        var cut = RenderAnimation(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Trigger, trigger)
            .Add(c => c.Shown, false)
            .Add(c => c.OnEntered, Callback(() => { })));

        var element = cut.Find(".nt-animation");
        element.Attributes.Select(a => a.Name).Where(n => n.StartsWith("data-nt-animation")).Should().BeEmpty();
        element.HasAttribute("inert").Should().BeFalse();
        element.QuerySelector("span")!.TextContent.Should().Be("Content");
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(AllTags))]
    public void Renders_Requested_Tag(NTAnimationTag tag, string expected) {
        var cut = RenderAnimation(p => p.Add(c => c.Tag, tag));

        var element = cut.Find(".nt-animation");
        element.LocalName.Should().Be(expected);
        element.QuerySelector("span")!.TextContent.Should().Be("Content");
    }

    [Fact]
    public void Additional_Attributes_Merge_With_Component_Class_And_Style() {
        var cut = RenderAnimation(p => p
            .Add(c => c.Distance, "12px")
            .AddUnmatched("class", "custom")
            .AddUnmatched("style", "color:red")
            .AddUnmatched("data-testid", "anim"));

        var element = cut.Find("[data-testid='anim']");
        element.ClassList.Should().Contain("custom").And.Contain("nt-animation").And.Contain("nt-animation-fade-in");
        var style = element.GetAttribute("style")!.Replace(" ", string.Empty);
        style.Should().Contain("color:red").And.Contain("--nt-animation-distance:12px");
    }

    [Fact]
    public async Task NotifyEnteredAsync_Invokes_OnEntered_Only() {
        int entered = 0, exited = 0;
        var cut = RenderAnimation(p => p.Add(c => c.OnEntered, Callback(() => entered++)).Add(c => c.OnExited, Callback(() => exited++)));

        await cut.InvokeAsync(() => cut.Instance.NotifyEnteredAsync());

        entered.Should().Be(1);
        exited.Should().Be(0);
    }

    [Fact]
    public async Task NotifyExitedAsync_Invokes_OnExited_Only() {
        int entered = 0, exited = 0;
        var cut = RenderAnimation(p => p.Add(c => c.OnEntered, Callback(() => entered++)).Add(c => c.OnExited, Callback(() => exited++)));

        await cut.InvokeAsync(() => cut.Instance.NotifyExitedAsync());

        exited.Should().Be(1);
        entered.Should().Be(0);
    }
}
