using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Window;

/// <summary>
///     Tests for <see cref="NTWindow" />.
/// </summary>
public class NTWindow_Tests : BunitContext {

    [Fact]
    public void Render_EmitsCompleteSsrFriendlyWindowMarkup() {
        var cut = Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .AddChildContent("<p>Quarterly results</p>"));

        var window = cut.Find("section.nt-window");

        window.GetAttribute("role").Should().Be("dialog");
        window.GetAttribute("aria-modal").Should().Be("false");
        window.GetAttribute("data-nt-window-dock-position").Should().Be("bottom-right");
        window.GetAttribute("data-nt-window-draggable").Should().Be("true");
        window.GetAttribute("data-nt-window-state").Should().Be("normal");
        cut.Find(".nt-window-title").TextContent.Should().Be("Report");
        cut.Find(".nt-window-content").InnerHtml.Should().Contain("Quarterly results");
        cut.FindAll(".nt-window-control").Should().HaveCount(3);
        cut.Find("button[aria-label='Minimize Report']").GetAttribute("aria-expanded").Should().Be("true");
        cut.Find("button[aria-label='Show Report fullscreen']").GetAttribute("aria-pressed").Should().Be("false");
        cut.Find("button[aria-label='Close Report']").Should().NotBeNull();
        cut.Find("nt-page-script").GetAttribute("src").Should().Be(NTWindow.JsModulePathValue);
        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void Floating_False_RendersInlineContentWithoutWindowContract() {
        var cut = Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .Add(component => component.Floating, false)
            .Add(component => component.State, NTWindowState.Minimized)
            .AddUnmatched("role", "region")
            .AddUnmatched("tabindex", "0")
            .AddChildContent("<p>Quarterly results</p>"));

        var section = cut.Find("section");

        section.GetAttribute("role").Should().Be("region");
        section.GetAttribute("tabindex").Should().Be("0");
        section.ClassList.Should().NotContain("nt-window").And.NotContain("nt-window-minimized");
        section.HasAttribute("data-nt-window").Should().BeFalse();
        section.HasAttribute("data-nt-window-state").Should().BeFalse();
        section.HasAttribute("aria-modal").Should().BeFalse();
        cut.FindAll(".nt-window-header").Should().BeEmpty();
        cut.Find(".nt-window-content-frame").HasAttribute("aria-hidden").Should().BeFalse();
        cut.Find(".nt-window-content").InnerHtml.Should().Contain("Quarterly results");
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Fact]
    public void Floating_Toggled_KeepsContentComponentInstance() {
        var cut = Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .Add(component => component.Floating, false)
            .Add(component => component.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<NTWindow>(0);
                builder.AddComponentParameter(1, nameof(NTWindow.Title), "Inner");
                builder.CloseComponent();
            })));
        var inner = cut.FindComponents<NTWindow>().Single(window => window.Instance.Title == "Inner").Instance;

        cut.Render(parameters => parameters.Add(component => component.Floating, true));

        cut.Find("section.nt-window[role='dialog']").GetAttribute("data-nt-window").Should().Be("true");
        cut.FindComponents<NTWindow>().Single(window => window.Instance.Title == "Inner").Instance.Should().BeSameAs(inner);
    }

    [Fact]
    public void Render_WithDockPosition_EmitsStaticDockingContract() {
        var cut = Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .Add(component => component.DockPosition, NTWindowDockPosition.TopCenter)
            .Add(component => component.State, NTWindowState.Minimized));

        var window = cut.Find("section.nt-window");
        window.GetAttribute("data-nt-window-dock-position").Should().Be("top-center");
        window.GetAttribute("data-nt-window-state").Should().Be("minimized");
        window.ClassList.Should().Contain("nt-window-minimized");
    }

    [Fact]
    public void Render_WhenDraggingIsDisabled_EmitsStaticBehaviorContract() {
        var cut = Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .Add(component => component.Draggable, false));

        cut.Find("section.nt-window").GetAttribute("data-nt-window-draggable").Should().Be("false");
        cut.Find("[data-nt-window-drag-handle]").Should().NotBeNull();
    }

    [Fact]
    public async Task MinimizeControl_TogglesContentWithoutRemovingIt() {
        var states = new List<NTWindowState>();
        var cut = Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .Add(component => component.StateChanged, (NTWindowState state) => states.Add(state))
            .AddChildContent("<p>Quarterly results</p>"));

        await cut.Find("button[aria-label='Minimize Report']").ClickAsync(new MouseEventArgs());

        var window = cut.Find("section.nt-window");
        window.ClassList.Should().Contain("nt-window-minimized");
        window.GetAttribute("data-nt-window-state").Should().Be("minimized");
        cut.Find(".nt-window-content-frame").GetAttribute("aria-hidden").Should().Be("true");
        cut.Find(".nt-window-content").TextContent.Should().Contain("Quarterly results");
        cut.Find("button[aria-label='Restore Report']").GetAttribute("aria-expanded").Should().Be("false");
        states.Should().Equal(NTWindowState.Minimized);

        await cut.Find("button[aria-label='Restore Report']").ClickAsync(new MouseEventArgs());

        cut.Find("section.nt-window").ClassList.Should().NotContain("nt-window-minimized");
        states.Should().Equal(NTWindowState.Minimized, NTWindowState.Normal);
        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task FullscreenControl_TogglesFullscreenState() {
        var cut = Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .AddChildContent("Content"));

        await cut.Find("button[aria-label='Show Report fullscreen']").ClickAsync(new MouseEventArgs());

        var window = cut.Find("section.nt-window");
        window.ClassList.Should().Contain("nt-window-fullscreen");
        window.GetAttribute("data-nt-window-state").Should().Be("fullscreen");
        cut.Find("button[aria-label='Restore Report']").GetAttribute("aria-pressed").Should().Be("true");

        await cut.Find("button[aria-label='Restore Report']").ClickAsync(new MouseEventArgs());

        cut.Find("section.nt-window").ClassList.Should().NotContain("nt-window-fullscreen");
        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task CloseControl_ClosesWindowAndInvokesCallbacks() {
        var openValues = new List<bool>();
        var closedCount = 0;
        var cut = Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .Add(component => component.OpenChanged, (bool open) => openValues.Add(open))
            .Add(component => component.OnClosed, () => closedCount++)
            .AddChildContent("Content"));

        await cut.Find("button[aria-label='Close Report']").ClickAsync(new MouseEventArgs());

        cut.FindAll("[data-nt-window]").Should().BeEmpty();
        openValues.Should().Equal(false);
        closedCount.Should().Be(1);
        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void Render_WithInvalidState_Throws() {
        var action = () => Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .Add(component => component.State, (NTWindowState)999));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Render_WithInvalidDockPosition_Throws() {
        var action = () => Render<NTWindow>(parameters => parameters
            .Add(component => component.Title, "Report")
            .Add(component => component.DockPosition, (NTWindowDockPosition)999));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
