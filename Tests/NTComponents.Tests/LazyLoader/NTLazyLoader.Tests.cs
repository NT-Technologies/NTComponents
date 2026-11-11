using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.LazyLoader;

public class NTLazyLoader_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/LazyLoader/NTLazyLoader.razor.js";
    private readonly BunitJSModuleInterop _module;

    public NTLazyLoader_Tests() {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _module = JSInterop.SetupModule(JsModulePath);
        _module.SetupVoid("onLoad", _ => true).SetVoidResult();
        _module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        _module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    private IRenderedComponent<NTLazyLoader> RenderLoader() =>
        Render<NTLazyLoader>(parameters => parameters.AddChildContent("<p class=\"lazy-content\">Expensive editor</p>"));

    [Fact]
    public void Renders_Only_The_Placeholder_Before_It_Is_Visible() {
        var cut = RenderLoader();

        cut.FindAll(".lazy-content").Should().BeEmpty();
        cut.Find(".nt-lazy-loader").GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Asks_The_Browser_To_Watch_The_Placeholder_On_First_Render() {
        var cut = RenderLoader();

        cut.WaitForAssertion(() => _module.Invocations["onLoad"].Should().ContainSingle());
    }

    [Fact]
    public async Task Renders_The_Content_In_Place_Of_The_Placeholder_Once_Shown() {
        var cut = RenderLoader();

        await cut.Instance.ShowAsync();

        cut.Find(".lazy-content").TextContent.Should().Be("Expensive editor");
        cut.FindAll(".nt-lazy-loader").Should().BeEmpty();
    }

    [Fact]
    public async Task Keeps_The_Content_When_Shown_Again() {
        var cut = RenderLoader();
        await cut.Instance.ShowAsync();

        await cut.Instance.ShowAsync();

        cut.FindAll(".lazy-content").Should().ContainSingle();
    }

    [Fact]
    public void Static_Ssr_Sends_The_Content_Inert_For_The_Page_Script_To_Show() {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderLoader();

        cut.Markup.Should().Contain("<template><p class=\"lazy-content\">Expensive editor</p></template>");
        cut.FindAll(".lazy-content").Should().BeEmpty();
        cut.Find("nt-page-script").GetAttribute("src").Should().Be(JsModulePath);
    }

    [Fact]
    public void Prerendering_For_An_Interactive_Page_Renders_Only_The_Placeholder() {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<NTLazyLoader>(parameters => parameters
            .SetAssignedRenderMode(RenderMode.InteractiveServer)
            .AddChildContent("<p class=\"lazy-content\">Expensive editor</p>"));

        cut.FindAll(".lazy-content").Should().BeEmpty();
        cut.FindAll(".nt-lazy-loader").Should().ContainSingle();
        cut.FindAll("template").Should().BeEmpty();
    }
}
