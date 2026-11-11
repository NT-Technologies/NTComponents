using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Snackbar;

namespace NTComponents.Tests.Snackbar;

/// <summary>
///     bUnit tests for <see cref="NTSnackbar" />.
/// </summary>
public class NTSnackbar_Tests : BunitContext {

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var cut = RenderSnackbarComponent();

        // Assert
        cut.Should().NotBeNull();
        cut.Instance.Should().NotBeNull();
    }

    [Fact]
    public void Render_RendersStaticHostAndModuleOnly() {
        // Arrange & Act
        var cut = RenderSnackbarComponent();

        // Assert
        var container = cut.Find(".nt-snackbar-container");
        container.GetAttribute("data-nt-snackbar-host").Should().Be("true");
        container.GetAttribute("popover").Should().Be("manual");
        cut.Find("nt-page-script").GetAttribute("src").Should().Be(NTSnackbar.JsModulePathValue);
        cut.FindAll(".nt-snackbar").Should().BeEmpty();
        cut.FindAll("script").Should().BeEmpty();
    }

    [Fact]
    public void Snackbar_DefaultsToBottomCenterPosition() {
        // Arrange & Act
        var cut = RenderSnackbarComponent();

        // Assert
        cut.Find(".nt-snackbar-container").ClassList.Should().Contain("nt-snackbar-bottom-center");
    }

    [Fact]
    public void Snackbar_UsesConfiguredPositionClass() {
        // Arrange & Act
        var cut = RenderSnackbarComponent(parameters => parameters.Add(p => p.Position, NTSnackbarPosition.TopRightCorner));

        // Assert
        var container = cut.Find(".nt-snackbar-container");
        container.ClassList.Should().Contain("nt-snackbar-top-right-corner");
        container.ClassList.Should().NotContain("nt-snackbar-bottom-center");
    }

    [Fact]
    public void RenderQueueScript_RendersHelperScriptWithConfiguredSnackbarOptions() {
        // Arrange & Act
        var cut = Render(NTSnackbar.RenderQueueScript(new NTSnackbarQueueScriptOptions {
            Message = "Photos deleted",
            ActionLabel = "Undo",
            Timeout = 0,
            ShowClose = true,
            BackgroundColor = NTColor.InverseSurface,
            TextColor = NTColor.InverseOnSurface,
            ActionColor = NTColor.InversePrimary,
            Id = "photos-deleted",
            Host = "snackbar-host"
        }));

        // Assert
        var script = cut.Find("script").TextContent;
        script.Should().Contain("const snackbar = {");
        script.Should().Contain("message: \"Photos deleted\"");
        script.Should().Contain("actionLabel: \"Undo\"");
        script.Should().Contain("timeout: 0");
        script.Should().Contain("showClose: true");
        script.Should().Contain("backgroundColor: \"var(--nt-color-inverse-surface)\"");
        script.Should().Contain("textColor: \"var(--nt-color-inverse-on-surface)\"");
        script.Should().Contain("actionColor: \"var(--nt-color-inverse-primary)\"");
        script.Should().Contain("id: \"photos-deleted\"");
        script.Should().Contain("host: \"snackbar-host\"");
        script.Should().Contain("if (window.NTSnackbar?.queueSnackbar)");
        script.Should().Contain("window.NTSnackbar.queueSnackbar(snackbar)");
        script.Should().Contain("(window.__ntSnackbarPendingQueue ??= []).push(snackbar)");
        script.Should().NotContain("setTimeout");
    }

    [Fact]
    public void RenderQueueScript_EncodesScriptBreakingCharacters() {
        // Arrange & Act
        var cut = Render(NTSnackbar.RenderQueueScript("</script><script>alert('bad')</script>"));

        // Assert
        var script = cut.Find("script").TextContent;
        script.Should().Contain("\\u003C/script\\u003E");
        script.Should().NotContain("</script>");
    }

    private IRenderedComponent<NTSnackbar> RenderSnackbarComponent(Action<ComponentParameterCollectionBuilder<NTSnackbar>>? configure = null) {
        return configure is null ? Render<NTSnackbar>() : Render<NTSnackbar>(configure);
    }
}
