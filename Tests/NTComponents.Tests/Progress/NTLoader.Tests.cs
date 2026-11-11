using System.Globalization;
using NTComponents;
using NTComponents.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace NTComponents.Tests.Progress;

/// <summary>
///     Unit tests for <see cref="NTLoader" />.
/// </summary>
public class NTLoader_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Progress/NTLoader.razor.js";
    private readonly BunitJSModuleInterop _module;

    public NTLoader_Tests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        _module = JSInterop.SetupModule(JsModulePath);
        _module.SetupVoid("onLoad", _ => true).SetVoidResult();
        _module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        _module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task Disposal_While_OnLoad_Is_Awaiting_Does_Not_Invoke_OnUpdate_With_A_Disposed_Reference() {
        var cut = Render<TestLoader>();
        cut.Instance.ClearIsolatedJsModule();
        var initialUpdateCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "onUpdate");
        var pendingOnLoad = _module.SetupVoid("onLoad", _ => true);

        var renderTask = cut.InvokeAsync(() => cut.Instance.InvokeOnAfterRenderAsync(firstRender: true));
        await Task.Yield();
        pendingOnLoad.Invocations.Should().ContainSingle();
        cut.Instance.Dispose();
        pendingOnLoad.SetVoidResult();
        await renderTask;

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "onUpdate").Should().Be(initialUpdateCount);
        JSInterop.Invocations
            .Where(invocation => invocation.Identifier == "onLoad" || invocation.Identifier == "onUpdate")
            .Should().OnlyContain(invocation => invocation.Arguments[1] != null);
    }

    [Fact]
    public void Additional_Attributes_Are_Applied_To_Root_Element() {
        var attrs = new Dictionary<string, object> {
            { "data-testid", "loader" },
            { "role", "status" }
        };

        var cut = Render<NTLoader>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("data-testid")!.Should().Be("loader");
        loader.GetAttribute("role")!.Should().Be("status");
    }

    [Fact]
    public void Aria_LabelledBy_Suppresses_Default_Aria_Label() {
        var cut = Render<NTLoader>(p => p.Add(c => c.AriaLabelledBy, "loading-label"));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("aria-labelledby")!.Should().Be("loading-label");
        loader.HasAttribute("aria-label").Should().BeFalse();
    }

    [Fact]
    public void Contained_Variant_Adds_Contained_Class_And_Color_Variable() {
        var cut = Render<NTLoader>(p => p
            .Add(c => c.Variant, NTLoaderVariant.Contained)
            .Add(c => c.ContainerColor, NTColor.SecondaryContainer));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("class")!.Should().Contain("nt-loader-contained");
        loader.GetAttribute("style")!.Should().Contain("--nt-loader-container-color:var(--nt-color-secondary-container)");
    }

    [Fact]
    public void Custom_Shapes_Are_Rendered_From_Provided_Sequence() {
        var cut = Render<NTLoader>(p => p.Add(c => c.Shapes, new[] {
            NTShapeType.Hexagon,
            NTShapeType.Puffy,
            NTShapeType.Gem
        }));
        var loader = cut.Find(".nt-loader");
        var shape = cut.Find("nt-shape.nt-loader-shape");

        loader.GetAttribute("data-shape-sequence")!.Should().Be("2 28 22");
        loader.GetAttribute("data-shape-interval-ms")!.Should().Be("650");
        loader.GetAttribute("data-transition-duration-ms")!.Should().Be("700");
        shape.GetAttribute("data-shape")!.Should().Be(((int)NTShapeType.Hexagon).ToString());
    }

    [Fact]
    public void Default_Render_Is_Accessible_Indeterminate_Progressbar() {
        var cut = Render<NTLoader>();
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("role")!.Should().Be("progressbar");
        loader.GetAttribute("aria-label")!.Should().Be("Loading");
        loader.HasAttribute("aria-valuenow").Should().BeFalse();
    }

    [Fact]
    public void Default_Render_Uses_One_Morphing_Shape_Mark() {
        var cut = Render<NTLoader>();

        var shape = cut.Find("nt-shape.nt-loader-shape");
        shape.Should().NotBeNull();
        shape.GetAttribute("data-animate-shape-changes")!.Should().Be("true");
        cut.FindAll(".nt-progress-fill").Should().BeEmpty();
    }

    [Fact]
    public void Default_Shapes_IncludeEveryShape_StartingWithSoftBurst() {
        var cut = Render<NTLoader>();

        var sequence = cut.Find(".nt-loader").GetAttribute("data-shape-sequence")!.Split(' ').Select(value => (NTShapeType)int.Parse(value, CultureInfo.InvariantCulture)).ToArray();

        sequence.Should().StartWith(NTShapeType.SoftBurst).And.OnlyHaveUniqueItems().And.BeEquivalentTo(Enum.GetValues<NTShapeType>());
        cut.Find("nt-shape").GetAttribute("data-shape").Should().Be(((int)NTShapeType.SoftBurst).ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Emits_Page_Script_For_Static_And_Interactive_Rendering() {
        var cut = Render<NTLoader>();

        cut.Instance.Should().BeAssignableTo<INTPageScriptComponent<NTLoader>>();
        cut.FindAll("nt-page-script")
            .Select(script => script.GetAttribute("src"))
            .Should()
            .ContainSingle("./_content/NTComponents/Progress/NTLoader.razor.js");
    }

    [Fact]
    public void Static_Render_Places_Page_Script_Inside_Its_Loader_For_Browser_Initialization() {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = Render<NTLoader>();

        cut.FindAll(".nt-loader > [hidden] > nt-page-script").Should().ContainSingle();
    }

    [Fact]
    public void Indicator_Color_Sets_Css_Variable() {
        var cut = Render<NTLoader>(p => p.Add(c => c.Color, NTColor.Tertiary));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("style")!.Should().Contain("--nt-loader-indicator-color:var(--nt-color-tertiary)");
        loader.GetAttribute("style")!.Should().NotContain("--nt-shape-content-background");
    }

    [Fact]
    public void Merges_Custom_Class_And_Style() {
        var attrs = new Dictionary<string, object> {
            { "class", "custom-loader" },
            { "style", "margin: 10px;" }
        };

        var cut = Render<NTLoader>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("class")!.Should().Contain("custom-loader");
        loader.GetAttribute("style")!.Should().Contain("margin: 10px;");
    }

    [Fact]
    public void Paused_Render_Removes_Animated_Class() {
        var cut = Render<NTLoader>(p => p.Add(c => c.Animate, false));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("class")!.Should().NotContain("nt-loader-animated");
        loader.GetAttribute("class")!.Should().NotContain("nt-loader-paused");
        cut.Find("nt-shape.nt-loader-shape").GetAttribute("data-animate-shape-changes")!.Should().Be("false");
    }

    [Fact]
    public void Show_False_Renders_Nothing() {
        var cut = Render<NTLoader>(p => p.Add(c => c.Show, false));

        cut.Markup.Should().BeEmpty();
    }

    [Theory]
    [InlineData(NTSize.Smallest, "nt-size-xs")]
    [InlineData(NTSize.Small, "nt-size-s")]
    [InlineData(NTSize.Medium, "nt-size-m")]
    [InlineData(NTSize.Large, "nt-size-l")]
    [InlineData(NTSize.Largest, "nt-size-xl")]
    public void Size_Adds_Correct_Size_Class(NTSize size, string expectedClass) {
        var cut = Render<NTLoader>(p => p.Add(c => c.Size, size));

        cut.Find(".nt-loader").GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Fact]
    public void Wrapper_Reserves_Rotated_Shape_Box() {
        var cut = Render<NTLoader>();
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("style")!.Should().NotContain("inline-size");
        loader.GetAttribute("style")!.Should().NotContain("block-size");
        var shape = cut.Find("nt-shape.nt-loader-shape");
        shape.HasAttribute("style").Should().BeFalse();
    }

    [Fact]
    public void Size_Value_Sets_Size_Css_Variable() {
        var cut = Render<NTLoader>(p => p.Add(c => c.SizeValue, "72px"));

        cut.Find(".nt-loader").GetAttribute("style")!.Should().Contain("--nt-loader-size:72px");
    }

    [Fact]
    public void Animation_Duration_Sets_Shape_Interval() {
        var cut = Render<NTLoader>(p => p.Add(c => c.AnimationDuration, TimeSpan.FromMilliseconds(900)));

        cut.Find(".nt-loader").GetAttribute("data-shape-interval-ms")!.Should().Be("900");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Animation_Duration_Clamps_Tiny_Intervals(int milliseconds) {
        var cut = Render<NTLoader>(p => p.Add(c => c.AnimationDuration, TimeSpan.FromMilliseconds(milliseconds)));

        cut.Find(".nt-loader").GetAttribute("data-shape-interval-ms")!.Should().Be("400");
    }

    private sealed class TestLoader : NTLoader {
        public Task InvokeOnAfterRenderAsync(bool firstRender) => base.OnAfterRenderAsync(firstRender);

        public void ClearIsolatedJsModule() =>
            typeof(NTLoader)
                .GetProperty(nameof(IsolatedJsModule))!
                .SetValue(this, null);
    }
}
