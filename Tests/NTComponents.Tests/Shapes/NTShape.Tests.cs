using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Shapes;

/// <summary>
///     Unit tests for <see cref="NTShape" />.
/// </summary>
public class NTShape_Tests : BunitContext {

    public NTShape_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var module = JSInterop.SetupModule("./_content/NTComponents/Shapes/NTShape.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Component_Implements_PageScript_Component_Interface() {
        var cut = RenderShape();

        cut.Instance.Should().BeAssignableTo<NTComponents.Interfaces.INTPageScriptComponent<NTShape>>();
        cut.Instance.Should().BeAssignableTo<IAsyncDisposable>();
        cut.Instance.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void JsModulePath_Returns_Correct_Path() {
        var cut = RenderShape();

        cut.Instance.JsModulePath.Should().Be("./_content/NTComponents/Shapes/NTShape.razor.js");
    }

    [Fact]
    public void PageScript_RenderFragment_Is_Included() {
        var cut = RenderShape();

        var pageScripts = cut.FindAll("nt-page-script");
        pageScripts.Should().ContainSingle();
        pageScripts[0].GetAttribute("src").Should().Be("./_content/NTComponents/Shapes/NTShape.razor.js");
    }

    [Fact]
    public void Renders_Initial_ClipPath_Content_And_DataAttributes() {
        var cut = RenderShape(parameters => parameters
            .Add(x => x.Shape, NTShapeType.Heart)
            .Add(x => x.AnimateShapeChanges, true)
            .Add(x => x.ChildContent, builder => builder.AddContent(0, "Shape demo")));

        var root = cut.Find("nt-shape");
        root.GetAttribute("data-shape").Should().Be(((int)NTShapeType.Heart).ToString());
        root.GetAttribute("data-animate-shape-changes").Should().Be("true");
        root.GetAttribute("data-transition-duration-ms").Should().Be("500");
        root.GetAttribute("data-transition-easing").Should().Be(((int)NTMotionEasing.Emphasized).ToString());

        var clipPath = cut.Find("clipPath");
        clipPath.GetAttribute("clipPathUnits").Should().Be("objectBoundingBox");
        clipPath.GetAttribute("id").Should().NotBeNullOrWhiteSpace();

        var path = cut.Find(".nt-shape-path");
        path.GetAttribute("d").Should().StartWith("M");
        path.GetAttribute("d").Should().Contain(" L");
        path.GetAttribute("d").Should().EndWith(" Z");

        var content = cut.Find(".nt-shape-content");
        content.TextContent.Should().Contain("Shape demo");
        content.GetAttribute("style").Should().Contain("clip-path:url(#");
    }

    [Fact]
    public void AdditionalAttributes_Are_Preserved_On_Root() {
        var cut = RenderShape(parameters => parameters
            .AddUnmatched("data-testid", "shape-root")
            .AddUnmatched("class", "custom-shape"));

        var root = cut.Find("nt-shape");
        root.GetAttribute("data-testid").Should().Be("shape-root");
        root.ClassName.Should().Contain("custom-shape");
        root.ClassName.Should().Contain("nt-shape");
    }

    [Fact]
    public void Changing_Shape_Updates_Rendered_Path_And_DataShape() {
        var cut = RenderShape(parameters => parameters.Add(x => x.Shape, NTShapeType.Circle));
        var originalPath = cut.Find(".nt-shape-path").GetAttribute("d");

        cut.Render(parameters => parameters.Add(x => x.Shape, NTShapeType.Burst));

        cut.Find("nt-shape").GetAttribute("data-shape").Should().Be(((int)NTShapeType.Burst).ToString());
        cut.Find(".nt-shape-path").GetAttribute("d").Should().NotBe(originalPath);
    }

    private IRenderedComponent<NTShape> RenderShape(Action<ComponentParameterCollectionBuilder<NTShape>>? configure = null) {
        return Render<NTShape>(parameters => configure?.Invoke(parameters));
    }
}
