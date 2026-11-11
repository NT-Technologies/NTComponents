using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace NTComponents.Tests.Form;

public class NTBrowserTimeZone_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/NTBrowserTimeZone.razor.js";

    public NTBrowserTimeZone_Tests() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Renders_Named_Hidden_Field_And_Page_Script_For_Static_Ssr() {
        SetRendererInfo(new RendererInfo("Static", false));

        var cut = Render<NTBrowserTimeZone>();
        var input = cut.Find("input[type=hidden]");

        input.GetAttribute("name").Should().Be("BrowserTimeZoneId");
        input.GetAttribute("value").Should().Be("UTC");
        input.GetAttribute("data-nt-browser-time-zone").Should().Be("true");
        cut.Find("nt-page-script").GetAttribute("src").Should().Be(JsModulePath);
    }

    [Fact]
    public void Initial_Value_Takes_Precedence_Over_Custom_Fallback() {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = Render<NTBrowserTimeZone>(parameters => parameters
            .Add(component => component.Value, "America/New_York")
            .Add(component => component.FallbackValue, "Etc/UTC")
            .Add(component => component.ElementName, "model.TimeZoneId")
            .AddUnmatched("form", "profile-form"));

        var input = cut.Find("input[type=hidden]");

        input.GetAttribute("value").Should().Be("America/New_York");
        input.GetAttribute("name").Should().Be("model.TimeZoneId");
        input.GetAttribute("form").Should().Be("profile-form");
    }

    [Fact]
    public void Null_Fallback_Preserves_Unknown_Value() {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = Render<NTBrowserTimeZone>(parameters => parameters.Add(component => component.FallbackValue, null));

        cut.Find("input[type=hidden]").HasAttribute("value").Should().BeFalse();
    }

    [Theory]
    [InlineData("America/Denver")]
    [InlineData(null)]
    public void Change_Invokes_ValueChanged_Once_With_The_Field_Value(string? fieldValue) {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        string? capturedValue = null;
        var callbackCount = 0;
        var cut = Render<NTBrowserTimeZone>(parameters => parameters
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => {
                capturedValue = value;
                callbackCount++;
            })));

        cut.Find("input[type=hidden]").Change(fieldValue);

        callbackCount.Should().Be(1);
        capturedValue.Should().Be(fieldValue);
    }

    [Fact]
    public void Interactive_Render_Loads_And_Updates_Module_Against_The_Hidden_Field() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var cut = Render<NTBrowserTimeZone>();

        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        JSInterop.VerifyInvoke("onLoad", 1);
        JSInterop.VerifyInvoke("onUpdate", 1);
        var lifecycleInvocations = JSInterop.Invocations.Where(invocation => invocation.Identifier is "onLoad" or "onUpdate").ToArray();
        lifecycleInvocations.Should().HaveCount(2);
        foreach (var invocation in lifecycleInvocations) {
            invocation.Arguments.Should().HaveCount(2);
            invocation.Arguments[0].Should().BeOfType<ElementReference>().Which.Should().Be(cut.Instance.Element);
            invocation.Arguments[1].Should().BeSameAs(cut.Instance.DotNetObjectRef);
        }
    }

    [Fact]
    public void Parameter_Update_Uses_Fallback_And_Updates_The_Existing_Module() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var cut = Render<NTBrowserTimeZone>(parameters => parameters
            .Add(component => component.Value, "America/Denver")
            .Add(component => component.FallbackValue, "Etc/UTC"));

        cut.Render(parameters => parameters
            .Add(component => component.Value, (string?)null)
            .Add(component => component.FallbackValue, "Etc/UTC"));

        cut.Find("input[type=hidden]").GetAttribute("value").Should().Be("Etc/UTC");
        JSInterop.VerifyInvoke("onLoad", 1);
        JSInterop.VerifyInvoke("onUpdate", 2);
    }

    [Fact]
    public async Task DisposeAsync_Notifies_The_Module_And_Releases_Interop_References() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var cut = Render<NTBrowserTimeZone>();
        var element = cut.Instance.Element;
        var dotNetObjectRef = cut.Instance.DotNetObjectRef;

        await cut.Instance.DisposeAsync();

        var invocation = JSInterop.Invocations.Should().ContainSingle(item => item.Identifier == "onDispose").Which;
        invocation.Arguments.Should().HaveCount(2);
        invocation.Arguments[0].Should().Be(element);
        invocation.Arguments[1].Should().BeSameAs(dotNetObjectRef);
        cut.Instance.IsolatedJsModule.Should().BeNull();
        cut.Instance.DotNetObjectRef.Should().BeNull();
    }
}
