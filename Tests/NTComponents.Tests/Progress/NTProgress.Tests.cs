using NTComponents;

namespace NTComponents.Tests.Progress;

/// <summary>
///     Unit tests for <see cref="NTProgress" />.
/// </summary>
public class NTProgress_Tests : BunitContext {

    [Fact]
    public void Additional_Attributes_Are_Applied_To_Root_Element() {
        var attrs = new Dictionary<string, object> {
            { "data-testid", "progress" },
            { "role", "status" }
        };

        var cut = Render<NTProgress>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("data-testid")!.Should().Be("progress");
        progress.GetAttribute("role")!.Should().Be("status");
    }

    [Fact]
    public void Aria_LabelledBy_Suppresses_Default_Aria_Label() {
        var cut = Render<NTProgress>(p => p.Add(c => c.AriaLabelledBy, "loading-label"));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("aria-labelledby")!.Should().Be("loading-label");
        progress.HasAttribute("aria-label").Should().BeFalse();
    }

    [Fact]
    public void Default_Render_Is_Indeterminate_Linear_Progressbar() {
        var cut = Render<NTProgress>();
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("role")!.Should().Be("progressbar");
        progress.GetAttribute("aria-label")!.Should().Be("Loading");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-linear");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-indeterminate");
        progress.HasAttribute("aria-valuenow").Should().BeFalse();
        cut.Find(".nt-progress-track").LocalName.Should().Be("div");
        cut.FindAll("nt-shape").Should().BeEmpty();
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Theory]
    [InlineData(NTProgressVariant.Linear, "nt-progress-linear", false, true, "div", 0, 0)]
    [InlineData(NTProgressVariant.Ring, "nt-progress-ring", false, false, "svg", 0, 0)]
    [InlineData(NTProgressVariant.LinearWave, "nt-progress-linear", true, true, "div", 1, 1)]
    [InlineData(NTProgressVariant.RingWave, "nt-progress-ring", true, false, "svg", 0, 8)]
    public void Determinate_Progress_Indicators_Render_Track(NTProgressVariant variant, string expectedClass, bool sineWave, bool expectedStop, string expectedTrackElement, int expectedSineWaveTrackCount, int expectedSineWavePathCount) {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, variant)
            .Add(c => c.Value, 25)
            .Add(c => c.Max, 50));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain(expectedClass);
        progress.GetAttribute("class")!.Should().Contain("nt-progress-determinate");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-started");
        if (sineWave) {
            progress.GetAttribute("class")!.Should().Contain("nt-progress-sine-wave");
        }
        else {
            progress.GetAttribute("class")!.Should().NotContain("nt-progress-sine-wave");
        }

        progress.GetAttribute("aria-valuemin")!.Should().Be("0");
        progress.GetAttribute("aria-valuemax")!.Should().Be("50");
        progress.GetAttribute("aria-valuenow")!.Should().Be("25");
        progress.GetAttribute("style")!.Should().Contain("--nt-progress-active-angle:180deg");
        progress.GetAttribute("style")!.Should().Contain("--nt-progress-active-value:50");
        progress.GetAttribute("style")!.Should().Contain("--nt-progress-active-percentage:50%");
        var track = cut.Find(".nt-progress-track");
        track.Should().NotBeNull();
        track.LocalName.Should().Be(expectedTrackElement);
        cut.Find(".nt-progress-active-track").Should().NotBeNull();
        cut.FindAll(".nt-progress-stop").Should().HaveCount(expectedStop ? 1 : 0);
        cut.FindAll(".nt-progress-sine-wave-track").Should().HaveCount(expectedSineWaveTrackCount);
        cut.FindAll(".nt-progress-sine-wave-path").Should().HaveCount(expectedSineWavePathCount);
        cut.FindAll("nt-shape").Should().BeEmpty();
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Theory]
    [InlineData(NTProgressVariant.Linear, "span", 1)]
    [InlineData(NTProgressVariant.Ring, "circle", 1)]
    public void Default_Progress_Indicators_Render_Standard_Geometry(NTProgressVariant variant, string expectedActiveElement, int expectedTrackSegments) {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, variant)
            .Add(c => c.Value, 50));

        cut.Find(".nt-progress-track").LocalName.Should().Be(variant == NTProgressVariant.Linear ? "div" : "svg");
        cut.Find(".nt-progress-active-track").LocalName.Should().Be(expectedActiveElement);
        cut.FindAll(".nt-progress-track-segment").Should().HaveCount(expectedTrackSegments);
    }

    [Fact]
    public void Linear_Progress_Uses_Separate_Track_Active_And_Stop_For_Gaps() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.Linear)
            .Add(c => c.Value, 50));

        cut.Find(".nt-progress-track").LocalName.Should().Be("div");
        cut.Find(".nt-progress-track-segment").LocalName.Should().Be("span");
        cut.Find(".nt-progress-active-track").LocalName.Should().Be("span");
        cut.Find(".nt-progress-stop").LocalName.Should().Be("span");
        cut.FindAll("mask").Should().BeEmpty();
        var style = cut.Find(".nt-progress").GetAttribute("style")!;
        style.Should().Contain("--nt-progress-active-value:50");
        style.Should().Contain("--nt-progress-active-percentage:50%");
        style.Should().NotContain("--nt-progress-linear-track-start");
        style.Should().NotContain("--nt-progress-linear-track-end");
        style.Should().NotContain("--nt-progress-linear-track-size");
    }

    [Fact]
    public void Linear_Wave_Progress_Uses_Flat_Linear_Geometry_With_Fixed_Spec_Wave() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.LinearWave)
            .Add(c => c.Value, 50));

        cut.Find(".nt-progress-track").LocalName.Should().Be("div");
        cut.Find(".nt-progress-track-segment").LocalName.Should().Be("span");
        cut.Find(".nt-progress-active-track").LocalName.Should().Be("span");
        cut.Find(".nt-progress-stop").LocalName.Should().Be("span");

        var wave = cut.Find(".nt-progress-sine-wave-svg");
        wave.GetAttribute("viewBox").Should().Be("0 0 4080 10");
        wave.GetAttribute("width").Should().Be("4080");
        wave.GetAttribute("height").Should().Be("10");

        var pathData = cut.Find(".nt-progress-sine-wave-path").GetAttribute("d")!;
        pathData.Should().StartWith("M -40 5");
        pathData.Should().Contain("C -35 2 -25 2 -20 5");
        pathData.Should().Contain("C -15 8 -5 8 0 5");
    }

    [Fact]
    public void Large_Linear_Wave_Progress_Keeps_Three_Dp_Amplitude() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.LinearWave)
            .Add(c => c.Size, NTSize.Largest)
            .Add(c => c.Value, 50));

        var wave = cut.Find(".nt-progress-sine-wave-svg");
        wave.GetAttribute("viewBox").Should().Be("0 0 4080 14");
        wave.GetAttribute("height").Should().Be("14");

        var pathData = cut.Find(".nt-progress-sine-wave-path").GetAttribute("d")!;
        pathData.Should().StartWith("M -40 7");
        pathData.Should().Contain("C -35 4 -25 4 -20 7");
        pathData.Should().Contain("C -15 10 -5 10 0 7");
    }

    [Fact]
    public void Ring_Wave_Progress_Uses_Circular_Spec_Wave() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.RingWave)
            .Add(c => c.Value, 100));

        var pathData = cut.Find(".nt-progress-sine-wave-path").GetAttribute("d")!;

        pathData.Should().StartWith("M 92.5 50 L ");
        pathData.Should().Contain("95.137 57.959");
        pathData.Split(" L ").Should().HaveCount(145);
        pathData.Should().EndWith("92.5 50");
    }

    [Fact]
    public void Ring_Wave_Progress_Uses_Ring_Progress_Contract_With_Wave_Indicator() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.RingWave)
            .Add(c => c.Value, 50));

        var progress = cut.Find(".nt-progress");
        var indicator = cut.Find(".nt-progress-sine-wave-path");

        progress.GetAttribute("style")!.Should().Contain("--nt-progress-active-value:50");
        indicator.LocalName.Should().Be("path");
        indicator.GetAttribute("class")!.Should().Contain("nt-progress-active-track");
        indicator.GetAttribute("class")!.Should().Contain("nt-progress-active-track-main");
        indicator.GetAttribute("class")!.Should().Contain("nt-progress-sine-wave-path");
        indicator.HasAttribute("pathLength").Should().BeFalse();
        indicator.HasAttribute("mask").Should().BeFalse();
        indicator.GetAttribute("style")!.Should().Contain("--nt-progress-ring-wave-frame-index:0");
        cut.FindAll(".nt-progress-sine-wave-path animate").Should().BeEmpty();
        cut.FindAll(".nt-progress-ring-wave-mask-track").Should().BeEmpty();
        cut.FindAll(".nt-progress-sine-wave-path").Should().HaveCount(8);
        cut.FindAll(".nt-progress-track-segment").Should().HaveCount(1);
        cut.FindAll(".nt-progress-active-track").Should().HaveCount(8);
    }

    [Fact]
    public void Ring_Wave_Progress_Does_Not_Animate_Wave_When_Animation_Is_Disabled() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.RingWave)
            .Add(c => c.Value, 50)
            .Add(c => c.Animate, false));

        cut.Find(".nt-progress-sine-wave-path").Should().NotBeNull();
        cut.FindAll(".nt-progress-sine-wave-path animate").Should().BeEmpty();
    }

    [Fact]
    public void Determinate_Ring_Wave_Progress_Does_Not_Use_Path_Data_Animation() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.RingWave)
            .Add(c => c.Value, 50));

        cut.Find(".nt-progress-sine-wave-path").Should().NotBeNull();
        cut.FindAll(".nt-progress-sine-wave-path animate[attributeName=d]").Should().BeEmpty();
    }

    [Fact]
    public void Linear_Wave_Progress_Can_Be_Indeterminate() {
        var cut = Render<NTProgress>(p => p.Add(c => c.Variant, NTProgressVariant.LinearWave));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain("nt-progress-linear");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-sine-wave");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-indeterminate");
        progress.GetAttribute("class")!.Should().NotContain("nt-progress-determinate");
        progress.HasAttribute("aria-valuenow").Should().BeFalse();

        cut.Find(".nt-progress-track").LocalName.Should().Be("div");
        cut.Find(".nt-progress-active-track").LocalName.Should().Be("span");
        cut.Find(".nt-progress-sine-wave-svg").Should().NotBeNull();
        cut.Find(".nt-progress-sine-wave-path").Should().NotBeNull();
        cut.FindAll(".nt-progress-stop").Should().BeEmpty();
    }

    [Fact]
    public void Complete_Progress_Adds_Complete_Class() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.Ring)
            .Add(c => c.Value, 100));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain("nt-progress-complete");
    }

    [Fact]
    public void Zero_Progress_Does_Not_Add_Started_Class() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.Linear)
            .Add(c => c.Value, 0));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain("nt-progress-determinate");
        progress.GetAttribute("aria-valuenow")!.Should().Be("0");
        progress.GetAttribute("class")!.Should().NotContain("nt-progress-started");
        progress.GetAttribute("class")!.Should().NotContain("nt-progress-complete");
    }

    [Theory]
    [InlineData(NTProgressVariant.Linear)]
    [InlineData(NTProgressVariant.Ring)]
    public void Default_Progress_Indicator_Can_Be_Indeterminate(NTProgressVariant variant) {
        var cut = Render<NTProgress>(p => p.Add(c => c.Variant, variant));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain("nt-progress-indeterminate");
        progress.GetAttribute("class")!.Should().NotContain("nt-progress-determinate");
        progress.HasAttribute("aria-valuemin").Should().BeFalse();
        progress.HasAttribute("aria-valuemax").Should().BeFalse();
        progress.HasAttribute("aria-valuenow").Should().BeFalse();
        cut.FindAll(".nt-progress-stop").Should().BeEmpty();
        cut.FindAll(".nt-progress-track-segment").Should().HaveCount(variant == NTProgressVariant.Ring ? 2 : 1);
        cut.FindAll(".nt-progress-active-track").Should().HaveCount(variant == NTProgressVariant.Ring ? 2 : 1);
    }

    [Fact]
    public void Null_Value_Renders_Indeterminate_Progress() {
        var cut = Render<NTProgress>(p => p.Add(c => c.Value, (double?)null));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain("nt-progress-indeterminate");
        progress.GetAttribute("class")!.Should().NotContain("nt-progress-determinate");
        progress.HasAttribute("aria-valuenow").Should().BeFalse();
    }

    [Fact]
    public void Ring_Wave_Progress_Can_Be_Indeterminate() {
        var cut = Render<NTProgress>(p => p.Add(c => c.Variant, NTProgressVariant.RingWave));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain("nt-progress-ring");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-sine-wave");
        progress.GetAttribute("class")!.Should().Contain("nt-progress-indeterminate");
        progress.GetAttribute("class")!.Should().NotContain("nt-progress-determinate");
        progress.HasAttribute("aria-valuenow").Should().BeFalse();

        cut.Find(".nt-progress-track").LocalName.Should().Be("svg");
        cut.FindAll(".nt-progress-track-segment").Should().HaveCount(2);
        cut.FindAll(".nt-progress-active-track").Should().HaveCount(8);
        cut.FindAll(".nt-progress-sine-wave-path").Should().HaveCount(8);
        cut.Find(".nt-progress-active-track-main").LocalName.Should().Be("path");
    }

    [Fact]
    public void Progress_Indicators_Do_Not_Import_Shape_Module() {
        JSInterop.Mode = JSRuntimeMode.Strict;

        var cut = Render<NTProgress>(p => p.Add(c => c.Variant, NTProgressVariant.Linear));

        cut.FindAll("nt-page-script").Should().BeEmpty();
        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "import");
    }

    [Fact]
    public void Indicator_Color_Sets_Css_Variable() {
        var cut = Render<NTProgress>(p => p.Add(c => c.ProgressColor, NTColor.Tertiary));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("style")!.Should().Contain("--nt-progress-indicator-color:var(--nt-color-tertiary)");
        progress.GetAttribute("style")!.Should().NotContain("--nt-shape-content-background");
    }

    [Fact]
    public void Track_Color_Sets_Css_Variable() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.Linear)
            .Add(c => c.TrackColor, NTColor.TertiaryContainer));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("style")!.Should().Contain("--nt-progress-track-color:var(--nt-color-tertiary-container)");
    }

    [Fact]
    public void Hidden_Track_Adds_Class_And_Removes_Stop() {
        var cut = Render<NTProgress>(p => p
            .Add(c => c.Variant, NTProgressVariant.Linear)
            .Add(c => c.Value, 50)
            .Add(c => c.TrackVisible, false));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain("nt-progress-track-hidden");
        cut.FindAll(".nt-progress-stop").Should().BeEmpty();
    }

    [Fact]
    public void Merges_Custom_Class_And_Style() {
        var attrs = new Dictionary<string, object> {
            { "class", "custom-progress" },
            { "style", "margin: 10px;" }
        };

        var cut = Render<NTProgress>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain("custom-progress");
        progress.GetAttribute("style")!.Should().Contain("margin: 10px;");
    }

    [Fact]
    public void Paused_Render_Removes_Animated_Class() {
        var cut = Render<NTProgress>(p => p.Add(c => c.Animate, false));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().NotContain("nt-progress-animated");
        progress.GetAttribute("class")!.Should().NotContain("nt-progress-paused");
    }

    [Fact]
    public void Show_False_Renders_Nothing() {
        var cut = Render<NTProgress>(p => p.Add(c => c.Show, false));

        cut.Markup.Should().BeEmpty();
    }

    [Theory]
    [InlineData(NTSize.Smallest, "nt-size-xs")]
    [InlineData(NTSize.Small, "nt-size-s")]
    [InlineData(NTSize.Medium, "nt-size-m")]
    [InlineData(NTSize.Large, "nt-size-l")]
    [InlineData(NTSize.Largest, "nt-size-xl")]
    public void Size_Adds_Correct_Size_Class(NTSize size, string expectedClass) {
        var cut = Render<NTProgress>(p => p.Add(c => c.Size, size));
        var progress = cut.Find(".nt-progress");

        progress.GetAttribute("class")!.Should().Contain(expectedClass);
        progress.GetAttribute("style")!.Should().NotContain("--nt-progress-size");
    }
}
