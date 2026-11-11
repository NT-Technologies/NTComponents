using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Toast;

namespace NTComponents.Tests.Toast;

/// <summary>
///     bUnit tests for <see cref="NTToast" />.
/// </summary>
public class NTToast_Tests : BunitContext {
    [Fact]
    public void Render_RendersStaticHostAndModuleOnly() {
        // Arrange & Act
        var cut = RenderToastComponent();

        // Assert
        var container = cut.Find(".nt-toast-container");
        container.GetAttribute("data-nt-toast-host").Should().Be("true");
        container.GetAttribute("popover").Should().Be("manual");
        cut.Find("nt-page-script").GetAttribute("src").Should().Be(NTToast.JsModulePathValue);
        cut.FindAll(".nt-toast").Should().BeEmpty();
        cut.FindAll("script").Should().BeEmpty();
    }

    [Fact]
    public void Toast_DefaultsToBottomRightPosition() {
        // Arrange & Act
        var cut = RenderToastComponent();

        // Assert
        cut.Find(".nt-toast-container").ClassList.Should().Contain("nt-toast-bottom-right-corner");
    }

    [Fact]
    public void Toast_UsesConfiguredPositionClass() {
        // Arrange & Act
        var cut = RenderToastComponent(parameters => parameters.Add(p => p.Position, NTToastPosition.TopLeftCorner));

        // Assert
        var container = cut.Find(".nt-toast-container");
        container.ClassList.Should().Contain("nt-toast-top-left-corner");
        container.ClassList.Should().NotContain("nt-toast-bottom-right-corner");
    }

    [Fact]
    public void RenderQueueScript_RendersHelperScriptWithConfiguredToastOptions() {
        // Arrange & Act
        var cut = Render(NTToast.RenderQueueScript(new NTToastQueueScriptOptions {
            Title = "Invalid authenticator code",
            Message = "Please double check your authenticator code.",
            Variant = NTToastVariant.Error,
            Timeout = 0,
            ShowClose = false,
            Icon = "",
            BackgroundColor = NTColor.ErrorContainer,
            TextColor = NTColor.OnErrorContainer,
            IconColor = NTColor.Error,
            Id = "auth-code-toast",
            Host = "toast-host"
        }));

        // Assert
        var script = cut.Find("script").TextContent;
        script.Should().Contain("const toast = {");
        script.Should().Contain("title: \"Invalid authenticator code\"");
        script.Should().Contain("message: \"Please double check your authenticator code.\"");
        script.Should().Contain("variant: \"error\"");
        script.Should().Contain("timeout: 0");
        script.Should().Contain("showClose: false");
        script.Should().Contain("icon: \"\"");
        script.Should().Contain("backgroundColor: \"var(--nt-color-error-container)\"");
        script.Should().Contain("textColor: \"var(--nt-color-on-error-container)\"");
        script.Should().Contain("iconColor: \"var(--nt-color-error)\"");
        script.Should().Contain("id: \"auth-code-toast\"");
        script.Should().Contain("host: \"toast-host\"");
        script.Should().Contain("if (window.NTToast?.queueToast)");
        script.Should().Contain("window.NTToast.queueToast(toast)");
        script.Should().Contain("(window.__ntToastPendingQueue ??= []).push(toast)");
        script.Should().NotContain("setTimeout");
    }

    [Fact]
    public void RenderQueueScript_EncodesScriptBreakingCharacters() {
        // Arrange & Act
        var cut = Render(NTToast.RenderQueueScript(new NTToastQueueScriptOptions {
            Title = "</script><script>alert('bad')</script>",
            Message = "Quoted \"message\""
        }));

        // Assert
        var script = cut.Find("script").TextContent;
        script.Should().Contain("\\u003C/script\\u003E");
        script.Should().Contain("\\u0022message\\u0022");
        script.Should().NotContain("</script>");
    }

    [Theory]
    [InlineData(NTToastVariant.Success, "success")]
    [InlineData(NTToastVariant.Info, "info")]
    [InlineData(NTToastVariant.Warning, "warning")]
    [InlineData(NTToastVariant.Error, "error")]
    [InlineData(NTToastVariant.Assert, "assert")]
    public void RenderVariantQueueScript_RendersExpectedVariant(NTToastVariant variant, string scriptVariant) {
        // Arrange & Act
        var cut = Render(RenderVariantQueueScript(variant));

        // Assert
        var script = cut.Find("script").TextContent;
        script.Should().Contain("title: \"Status\"");
        script.Should().Contain($"variant: \"{scriptVariant}\"");
    }

    [Fact]
    public void RenderVariantQueueScript_OptionsOverloadKeepsConfigurationAndOverridesVariant() {
        // Arrange & Act
        var cut = Render(NTToast.RenderErrorQueueScript(new NTToastQueueScriptOptions {
            Title = "Invalid",
            Message = "Try again",
            Variant = NTToastVariant.Success,
            Id = "invalid-toast"
        }));

        // Assert
        var script = cut.Find("script").TextContent;
        script.Should().Contain("title: \"Invalid\"");
        script.Should().Contain("message: \"Try again\"");
        script.Should().Contain("id: \"invalid-toast\"");
        script.Should().Contain("variant: \"error\"");
        script.Should().NotContain("variant: \"success\"");
    }

    private IRenderedComponent<NTToast> RenderToastComponent(Action<ComponentParameterCollectionBuilder<NTToast>>? configure = null) {
        return configure is null ? Render<NTToast>() : Render<NTToast>(configure);
    }

    private static RenderFragment RenderVariantQueueScript(NTToastVariant variant) {
        return variant switch {
            NTToastVariant.Success => NTToast.RenderSuccessQueueScript("Status"),
            NTToastVariant.Info => NTToast.RenderInfoQueueScript("Status"),
            NTToastVariant.Warning => NTToast.RenderWarningQueueScript("Status"),
            NTToastVariant.Error => NTToast.RenderErrorQueueScript("Status"),
            NTToastVariant.Assert => NTToast.RenderAssertQueueScript("Status"),
            _ => NTToast.RenderQueueScript("Status")
        };
    }
}
